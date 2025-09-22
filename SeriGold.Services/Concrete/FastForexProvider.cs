using Microsoft.Extensions.Configuration;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;
using System.Text.Json;

namespace SeriGold.Services.Concrete
{
    /// fastforex.io — saniyelik FX
    public sealed class FastForexProvider : IRateProvider
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public FastForexProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(2); // kısa ve agresif; saniyelik akış
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+seridoviz)");

            _baseUrl = cfg.GetValue<string>("Sources:FastForex:Base") ?? "https://api.fastforex.io";
            _apiKey = cfg.GetValue<string>("Sources:FastForex:ApiKey")
                       ?? throw new InvalidOperationException("Sources:FastForex:ApiKey missing");
        }

        public async Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var list = new List<RateTick>();

            // USD bazlı: TRY, JPY, EUR
            await FetchMulti("USD", new[] { "TRY", "JPY", "EUR" }, now, list, ct);

            // EUR bazlı: TRY, USD
            await FetchMulti("EUR", new[] { "TRY", "USD" }, now, list, ct);

            // EUR/USD yoksa türet
            if (!list.Any(x => x.Symbol == "EUR/USD"))
            {
                var eurTry = list.FirstOrDefault(x => x.Symbol == "EUR/TRY")?.Mid;
                var usdTry = list.FirstOrDefault(x => x.Symbol == "USD/TRY")?.Mid;
                if (eurTry is > 0 && usdTry is > 0)
                    list.Add(MakeFromMid("EUR/USD", eurTry.Value / usdTry.Value, now));
            }
            return list;
        }

        private async Task FetchMulti(string from, IEnumerable<string> toList, DateTime ts, List<RateTick> outp, CancellationToken ct)
        {
            var toCsv = string.Join(",", toList);
            var url = $"{_baseUrl}/fetch-multi?from={from}&to={toCsv}&api_key={_apiKey}";
            using var r = await _http.GetAsync(url, ct);
            r.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("results", out var results)) return;

            foreach (var kv in results.EnumerateObject())
            {
                if (kv.Value.ValueKind != JsonValueKind.Number) continue;
                var mid = (decimal)kv.Value.GetDouble();
                outp.Add(MakeFromMid($"{from}/{kv.Name}", mid, ts));
            }
        }

        private static RateTick MakeFromMid(string sym, decimal mid, DateTime ts)
        {
            var isTry = sym.EndsWith("/TRY", StringComparison.Ordinal) || sym.StartsWith("TRY/", StringComparison.Ordinal);
            var sp = isTry ? 0.0006m : 0.0002m;
            return new RateTick(sym, mid * (1m - sp), mid * (1m + sp), mid, ts, "fastforex");
        }
    }
}

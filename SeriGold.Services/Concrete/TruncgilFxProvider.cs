using Microsoft.Extensions.Configuration;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;
using System.Text.Json;

namespace SeriGold.Services.Concrete
{
    // Dakikalık “serbest piyasa” çıpası + yedek veri
    public sealed class TruncgilFxProvider : IRateProvider
    {
        private readonly HttpClient _http;
        private readonly string _url;

        public TruncgilFxProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _url = cfg.GetValue<string>("Sources:Truncgil:Url")!;
            _http.Timeout = TimeSpan.FromSeconds(4);
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+seridoviz)");
        }

        public async Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default)
        {
            using var r = await _http.GetAsync(_url, ct);
            r.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));

            var now = DateTime.UtcNow;
            var list = new List<RateTick>();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals("Update_Date")) continue;
                if (prop.Value.ValueKind != JsonValueKind.Object) continue;

                var o = prop.Value;
                if (!o.TryGetProperty("Type", out var t) || t.GetString() != "Currency") continue;

                var code = prop.Name.ToUpperInvariant(); // USD, EUR...
                var symbol = $"{code}/TRY";

                decimal? bid = o.TryGetProperty("Buying", out var b) ? b.GetDecimal() : (decimal?)null;
                decimal? ask = o.TryGetProperty("Selling", out var s) ? s.GetDecimal() : (decimal?)null;
                decimal mid = (bid, ask) switch
                {
                    (decimal bb, decimal aa) => (bb + aa) / 2m,
                    (decimal bb, null) => bb,
                    (null, decimal aa) => aa,
                    _ => 0m
                };
                decimal? ch = o.TryGetProperty("Change", out var c) ? c.GetDecimal() : (decimal?)null;

                list.Add(new RateTick(symbol, bid, ask, mid, now, "truncgil", ch));
            }
            return list;
        }
    }
}

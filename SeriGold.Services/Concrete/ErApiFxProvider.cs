// Services/Concrete/ErApiFxProvider.cs
using System.Text.Json;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;

namespace SeriGold.Services.Concrete
{
    // USD bazlı geniş çapraz desteği (yedek kaynak)
    public sealed class ErApiFxProvider : IRateProvider
    {
        private readonly HttpClient _http;
        public ErApiFxProvider(HttpClient http)
        {
            _http = http;
            if (_http.Timeout == default) _http.Timeout = TimeSpan.FromSeconds(8);
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+serigold)");
        }

        private sealed class ErApiResponse
        {
            public string? result { get; set; }
            public string? base_code { get; set; }
            public Dictionary<string, decimal>? rates { get; set; }
            public long time_last_update_unix { get; set; }
        }

        public async Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default)
        {
            using var res = await _http.GetAsync("https://open.er-api.com/v6/latest/USD", ct);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync(ct);

            var obj = JsonSerializer.Deserialize<ErApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (obj?.rates == null || obj.result != "success" || obj.base_code != "USD")
                return Array.Empty<RateTick>();

            var t = DateTimeOffset.FromUnixTimeSeconds(obj.time_last_update_unix == 0
                        ? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        : obj.time_last_update_unix).UtcDateTime;

            var list = new List<RateTick>();
            const decimal spread = 0.001m;

            obj.rates.TryGetValue("TRY", out var usdTry); // USD/TRY

            foreach (var (ccy, per) in obj.rates)
            {
                if (ccy == "USD" || per <= 0) continue;

                list.Add(New($"USD/{ccy}", per, t, "erapi", spread));         // USD/CCY
                list.Add(New($"{ccy}/USD", 1m / per, t, "erapi", spread));    // CCY/USD

                if (usdTry > 0 && ccy != "TRY")
                {
                    // CCY/TRY = USD/TRY ÷ USD/CCY
                    var midTry = usdTry / per;
                    list.Add(New($"{ccy}/TRY", midTry, t, "erapi", spread));
                }
            }

            if (usdTry > 0)
            {
                list.Add(New("USD/TRY", usdTry, t, "erapi", spread));
                list.Add(New("TRY/USD", 1m / usdTry, t, "erapi", spread));
            }

            return list;

            static RateTick New(string symbol, decimal mid, DateTime ts, string src, decimal sp)
                => new(symbol, mid * (1m - sp), mid * (1m + sp), mid, ts, src);
        }
    }
}

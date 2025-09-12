// Services/Concrete/BinanceFxProvider.cs
using System.Globalization;
using System.Text.Json;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;

namespace SeriGold.Services.Concrete
{
    // Binance spot → canlı USD/TRY (USDT/TRY), EUR/USD, EUR/TRY
    public sealed class BinanceFxProvider : IRateProvider
    {
        private readonly HttpClient _http;
        public BinanceFxProvider(HttpClient http)
        {
            _http = http;
            if (_http.Timeout == default) _http.Timeout = TimeSpan.FromSeconds(8);
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+serigold)");
        }

        private static async Task<decimal?> Price(HttpClient http, string symbol, CancellationToken ct)
        {
            using var r = await http.GetAsync($"https://api.binance.com/api/v3/ticker/price?symbol={symbol}", ct);
            if (!r.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("price", out var p)) return null;
            return decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        public async Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default)
        {
            var ts = DateTime.UtcNow;
            const decimal sp = 0.0007m; // görsel küçük spread
            var list = new List<RateTick>();

            var usdtTry = await Price(_http, "USDTTRY", ct);   // 1 USDT kaç TRY
            if (usdtTry is > 0m)
            {
                var mid = usdtTry.Value;                       // USD≈USDT
                list.Add(new RateTick("USD/TRY", mid * (1m - sp), mid * (1m + sp), mid, ts, "binance"));
                var inv = 1m / mid;
                list.Add(new RateTick("TRY/USD", inv * (1m - sp), inv * (1m + sp), inv, ts, "binance"));
            }

            var eurUsdt = await Price(_http, "EURUSDT", ct);
            if (eurUsdt is > 0m)
            {
                var eurUsd = eurUsdt.Value;
                list.Add(new RateTick("EUR/USD", eurUsd * (1m - sp), eurUsd * (1m + sp), eurUsd, ts, "binance"));
                if (usdtTry is > 0m)
                {
                    var eurTry = eurUsd * usdtTry.Value;
                    list.Add(new RateTick("EUR/TRY", eurTry * (1m - sp), eurTry * (1m + sp), eurTry, ts, "binance"));
                }
            }

            return list;
        }
    }
}

// Services/Concrete/MetalsAnyProvider.cs
using System.Globalization;
using System.Text.Json;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;

namespace SeriGold.Services.Concrete
{
    // XAU/XAG (+ XPT/XPD) çok kaynaktan (goldprice → metals.live → exchangerate.host → stooq)
    public sealed class MetalsAnyProvider : IRateProvider
    {
        private readonly HttpClient _http;
        public MetalsAnyProvider(HttpClient http)
        {
            _http = http;
            if (_http.Timeout == default) _http.Timeout = TimeSpan.FromSeconds(8);
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+serigold)");
        }

        public async Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default)
        {
            var data = await TryGoldPrice(ct)
                    ?? await TryMetalsLive(ct)
                    ?? await TryExchangerateHost(ct)
                    ?? await TryStooq(ct);

            if (data == null || data.Count == 0) return Array.Empty<RateTick>();

            var now = DateTime.UtcNow;
            const decimal sp = 0.001m;

            return data.Select(kv =>
            {
                var mid = kv.Value;
                return new RateTick(kv.Key, mid * (1m - sp), mid * (1m + sp), mid, now, "metals");
            }).ToList();
        }

        // goldprice.org
        private async Task<Dictionary<string, decimal>?> TryGoldPrice(CancellationToken ct)
        {
            try
            {
                using var res = await _http.GetAsync("https://data-asg.goldprice.org/dbXRates/USD", ct);
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync(ct);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                    return null;

                var x = items[0];
                decimal? xau = x.TryGetProperty("xauPrice", out var p1) ? p1.GetDecimal() : (decimal?)null;
                decimal? xag = x.TryGetProperty("xagPrice", out var p2) ? p2.GetDecimal() : (decimal?)null;

                var d = new Dictionary<string, decimal>();
                if (xau is > 0) d["XAU/USD"] = xau.Value;
                if (xag is > 0) d["XAG/USD"] = xag.Value;
                return d.Count > 0 ? d : null;
            }
            catch { return null; }
        }

        // metals.live
        private async Task<Dictionary<string, decimal>?> TryMetalsLive(CancellationToken ct)
        {
            try
            {
                using var res = await _http.GetAsync("https://api.metals.live/v1/spot", ct);
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync(ct);

                var arr = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (arr == null || arr.Count == 0) return null;

                decimal? gold = null, silver = null, platinum = null, palladium = null;
                foreach (var m in arr)
                {
                    foreach (var (k, v) in m)
                    {
                        var key = k.ToLowerInvariant();
                        if (key.Contains("gold")) gold = v;
                        else if (key.Contains("silver")) silver = v;
                        else if (key.Contains("platinum")) platinum = v;
                        else if (key.Contains("palladium")) palladium = v;
                    }
                }

                var d = new Dictionary<string, decimal>();
                if (gold is > 0) d["XAU/USD"] = gold.Value;
                if (silver is > 0) d["XAG/USD"] = silver.Value;
                if (platinum is > 0) d["XPT/USD"] = platinum.Value;
                if (palladium is > 0) d["XPD/USD"] = palladium.Value;
                return d.Count > 0 ? d : null;
            }
            catch { return null; }
        }

        // exchangerate.host (XAU/XAG için)
        private async Task<Dictionary<string, decimal>?> TryExchangerateHost(CancellationToken ct)
        {
            try
            {
                async Task<decimal?> One(string metal)
                {
                    var url = $"https://api.exchangerate.host/latest?base={metal}&symbols=USD";
                    using var r = await _http.GetAsync(url, ct);
                    r.EnsureSuccessStatusCode();
                    var j = await r.Content.ReadAsStringAsync(ct);
                    using var d = JsonDocument.Parse(j);
                    if (!d.RootElement.TryGetProperty("rates", out var rates)) return null;
                    if (!rates.TryGetProperty("USD", out var usd)) return null;
                    return usd.GetDecimal(); // USD per 1 metal
                }

                var result = new Dictionary<string, decimal>();
                var xau = await One("XAU"); if (xau is > 0) result["XAU/USD"] = xau.Value;
                var xag = await One("XAG"); if (xag is > 0) result["XAG/USD"] = xag.Value;
                try { var xpt = await One("XPT"); if (xpt is > 0) result["XPT/USD"] = xpt.Value; } catch { }
                try { var xpd = await One("XPD"); if (xpd is > 0) result["XPD/USD"] = xpd.Value; } catch { }
                return result.Count > 0 ? result : null;
            }
            catch { return null; }
        }

        // stooq (yedek)
        private async Task<Dictionary<string, decimal>?> TryStooq(CancellationToken ct)
        {
            try
            {
                async Task<string> GetCsv(string baseUrl)
                {
                    var url = $"{baseUrl}/q/l/?s=xauusd,xagusd,xptusd,xpdusd&f=sd2t2ohlcv&h&e=csv";
                    using var res = await _http.GetAsync(url, ct);
                    res.EnsureSuccessStatusCode();
                    return await res.Content.ReadAsStringAsync(ct);
                }

                string csv;
                try { csv = await GetCsv("https://stooq.com"); }
                catch { csv = await GetCsv("http://stooq.com"); }

                var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                using var sr = new StringReader(csv);
                _ = sr.ReadLine(); // header

                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    var cols = line.Split(',', StringSplitOptions.TrimEntries);
                    if (cols.Length < 7) continue;
                    if (!decimal.TryParse(cols[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var last)) continue;

                    var sym = cols[0].ToLowerInvariant() switch
                    {
                        "xauusd" => "XAU/USD",
                        "xagusd" => "XAG/USD",
                        "xptusd" => "XPT/USD",
                        "xpdusd" => "XPD/USD",
                        _ => cols[0].ToUpperInvariant()
                    };
                    dict[sym] = last;
                }

                return dict.Count > 0 ? dict : null;
            }
            catch { return null; }
        }
    }
}

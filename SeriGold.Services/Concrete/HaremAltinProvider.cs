using Microsoft.Extensions.Configuration;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;
using System.Globalization;
using System.Text.Json;

namespace SeriGold.Services.Concrete
{
    public sealed class HaremAltinProvider : IRateProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public HaremAltinProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http; _cfg = cfg;
            _http.Timeout = TimeSpan.FromSeconds(6);
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+seridoviz)");

            _http.DefaultRequestHeaders.TryAddWithoutValidation("x-rapidapi-host", cfg["Sources:Harem:Host"]!);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("x-rapidapi-key", cfg["Sources:Harem:Key"]!);
            _http.BaseAddress = new Uri(cfg["Sources:Harem:Base"]!);
        }

        private static decimal? P(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
            return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static string? Map(string k)
        {
            k = k.Trim().ToUpperInvariant();
            return k switch
            {
                "GRAM ALTIN" or "HAS ALTIN" or "HAS ALTIN " => "ALTIN/GRAM",
                "AYAR22" or "22 AYAR" or "AYAR22/GRAM" => "AYAR22/GRAM",
                "AYAR14" or "14 AYAR" or "AYAR14/GRAM" => "AYAR14/GRAM",

                "YENİ ÇEYREK" => "YENİ ÇEYREK",
                "ESKİ ÇEYREK" => "ESKİ ÇEYREK",
                "YENİ YARIM" => "YENİ YARIM",
                "ESKİ YARIM" => "ESKİ YARIM",
                "YENİ TAM" => "YENİ TAM",
                "ESKİ TAM" => "ESKİ TAM",
                "YENİ ATA" => "YENİ ATA",
                "ESKİ ATA" => "ESKİ ATA",
                "YENİ ATA5" => "YENİ ATA5",
                "ESKİ ATA5" => "ESKİ ATA5",
                "YENİ GREMSE" => "YENİ GREMSE",
                "ESKİ GREMSE" => "ESKİ GREMSE",
                _ => null
            };
        }

        public async Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default)
        {
            var endpoint = _cfg["Sources:Harem:Endpoint"]!;
            using var r = await _http.GetAsync(endpoint, ct);
            r.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("data", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return Array.Empty<RateTick>();

            var now = DateTime.UtcNow;
            var list = new List<RateTick>();

            foreach (var x in arr.EnumerateArray())
            {
                var key = x.TryGetProperty("key", out var k) ? k.GetString() : null;
                var sym = key != null ? Map(key) : null;
                if (sym is null) continue;

                var bid = x.TryGetProperty("buy", out var b) ? P(b.GetString()) : null;
                var ask = x.TryGetProperty("sell", out var s) ? P(s.GetString()) : null;

                var mid = (bid, ask) switch
                {
                    (decimal bb, decimal aa) => (bb + aa) / 2m,
                    (decimal bb, null) => bb,
                    (null, decimal aa) => aa,
                    _ => 0m
                };

                decimal? ch = null;
                if (x.TryGetProperty("percent", out var pc) && pc.ValueKind is JsonValueKind.String)
                {
                    if (decimal.TryParse(pc.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                        ch = v;
                }

                list.Add(new RateTick(sym, bid, ask, mid, now, "harem", ch));
            }
            return list;
        }
    }
}

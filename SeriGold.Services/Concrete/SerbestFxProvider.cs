using Microsoft.Extensions.Configuration;
using SeriGold.DTO.Rates;
using SeriGold.Services.Abstracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SeriGold.Services.Concrete
{
    /// Serbest piyasa çıpası (USD/TRY, EUR/TRY).
    /// Kaynak: basit, free JSON (anahtarsız). “serbest” olarak işaretlenir.
    public sealed class SerbestFxProvider : IRateProvider
    {
        private readonly HttpClient _http;
        private readonly string _url;

        public SerbestFxProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(4);
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("SeriGold/1.0 (+seridoviz)");

            // İstersen burada kendi endpoint’ini kullan.
            _url = cfg.GetValue<string>("Sources:SerbestPiyasa:Url")
                 ?? "https://api.genelpara.com/embed/para-birimleri.json";
        }

        public async Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default)
        {
            try
            {
                using var r = await _http.GetAsync(_url, ct);
                r.EnsureSuccessStatusCode();

                var json = await r.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                var now = DateTime.UtcNow;
                var outp = new List<RateTick>();

                TryAdd("USD", "USD/TRY");
                TryAdd("EUR", "EUR/TRY");

                return outp;

                void TryAdd(string code, string symbol)
                {
                    if (!doc.RootElement.TryGetProperty(code, out var o)) return;

                    // Bu endpoint "alis/satis" string verir.
                    decimal? bid = TryNum(o, "alis");
                    decimal? ask = TryNum(o, "satis");
                    var mid = (bid, ask) switch
                    {
                        (decimal bb, decimal aa) => (bb + aa) / 2m,
                        (decimal bb, null) => bb,
                        (null, decimal aa) => aa,
                        _ => 0m
                    };
                    outp.Add(new RateTick(symbol, bid, ask, mid, now, "serbest"));
                }

                static decimal? TryNum(JsonElement el, string prop)
                {
                    if (!el.TryGetProperty(prop, out var v)) return null;
                    var s = v.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return null;
                    // "41.275,60" vb. senaryolar için temizlik:
                    s = s.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
                    return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
                }
            }
            catch
            {
                return Array.Empty<RateTick>();
            }
        }
    }
}

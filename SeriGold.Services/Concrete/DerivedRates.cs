using SeriGold.DTO.Rates;

namespace SeriGold.Services.Concrete
{
    public static class DerivedRates
    {
        private const decimal OunceToGram = 31.1034768m;

        private static readonly IReadOnlyDictionary<string, (string Title, decimal BuyCoef, decimal SellCoef, int Dp)> CoinCoef
            = new Dictionary<string, (string, decimal, decimal, int)>
            {
                ["CEYREK_YENI"] = ("YENİ ÇEYREK", 1.6300m, 1.6350m, 0),
                ["CEYREK_ESKI"] = ("ESKİ ÇEYREK", 1.6000m, 1.6120m, 0),
                ["YARIM_YENI"] = ("YENİ YARIM", 3.2600m, 3.2700m, 0),
                ["YARIM_ESKI"] = ("ESKİ YARIM", 3.2050m, 3.2150m, 0),
                ["TEK_YENI"] = ("YENİ TAM", 6.5000m, 6.5100m, 0),
                ["TEK_ESKI"] = ("ESKİ TAM", 6.4300m, 6.4500m, 0),
                ["ATA_YENI"] = ("YENİ ATA", 6.6600m, 6.6850m, 0),
                ["ATA_ESKI"] = ("ESKİ ATA", 6.6550m, 6.6800m, 0),
                ["GREMESE_YENI"] = ("YENİ GREMSE", 16.1900m, 16.2500m, 0),
                ["GREMESE_ESKI"] = ("ESKİ GREMSE", 16.0500m, 16.1500m, 0),
                ["ATA5_YENI"] = ("YENİ ATA5", 33.1500m, 33.3500m, 0),
                ["ATA5_ESKI"] = ("ESKİ ATA5", 33.1000m, 33.2000m, 0),
            };

        private static readonly IReadOnlyDictionary<string, (string Title, decimal BidCoef, decimal AskCoef, int Dp)> KaratCoef
            = new Dictionary<string, (string, decimal, decimal, int)>
            {
                ["AYAR22/GRAM"] = ("AYAR22/GRAM", 0.9090m, 0.9530m, 2),
                ["AYAR14/GRAM"] = ("AYAR14/GRAM", 0.5480m, 0.7525m, 2),
            };

        public static IEnumerable<RateTick> Build(IReadOnlyList<RateTick> src)
        {
            var now = DateTime.UtcNow;
            var outp = new List<RateTick>();

            bool Has(string s) => src.Any(z => z.Symbol == s) || outp.Any(z => z.Symbol == s);
            RateTick? Find(string s) => src.FirstOrDefault(x => x.Symbol == s);

            var usdTry = Find("USD/TRY")?.Mid;
            var eurTry = Find("EUR/TRY")?.Mid;
            var jpyTry = Find("JPY/TRY")?.Mid;
            var xauUsd = Find("XAU/USD")?.Mid;
            var xagUsd = Find("XAG/USD")?.Mid;

            // ALTIN/GRAM (HAS) → XAU/USD ve tersi
            var gramHas = Find("ALTIN/GRAM");
            if (gramHas is null && xauUsd is > 0 && usdTry is > 0 && !Has("ALTIN/GRAM"))
            {
                var mid = (xauUsd.Value / OunceToGram) * usdTry.Value;
                outp.Add(New("ALTIN/GRAM", mid, now, 0.0010m));
                gramHas = outp.Last();
            }
            if (!Has("XAU/USD") && xauUsd is null && gramHas?.Mid is > 0 && usdTry is > 0)
            {
                var mid = gramHas.Mid * OunceToGram / usdTry.Value;
                outp.Add(New("XAU/USD", mid, now, 0.0010m));
            }

            // Ayar + Sarrafiye (kaynak yoksa)
            if (gramHas is not null)
            {
                foreach (var kv in KaratCoef.Values)
                {
                    if (Has(kv.Title)) continue;
                    var bid = Math.Round(gramHas.Bid.GetValueOrDefault() * kv.BidCoef, kv.Dp);
                    var ask = Math.Round(gramHas.Ask.GetValueOrDefault() * kv.AskCoef, kv.Dp);
                    outp.Add(new RateTick(kv.Title, bid, ask, (bid + ask) / 2m, now, "derived"));
                }
                var hasBid = gramHas.Bid.GetValueOrDefault();
                var hasAsk = gramHas.Ask.GetValueOrDefault();
                foreach (var kv in CoinCoef.Values)
                {
                    if (Has(kv.Title)) continue;
                    var bid = kv.BuyCoef * hasBid;
                    var ask = kv.SellCoef * hasAsk;
                    outp.Add(new RateTick(kv.Title, bid, ask, (bid + ask) / 2m, now, "derived"));
                }
            }

            // KG ALTIN ve EUR/KG
            if (!Has("USD/KG(ALTIN)") && xauUsd is > 0)
            {
                var usdKg = xauUsd.Value * (1000m / OunceToGram);
                outp.Add(New("USD/KG(ALTIN)", usdKg, now, 0.0010m));

                if (!Has("EUR/KG(ALTIN)") && eurTry is > 0 && usdTry is > 0)
                {
                    var eurUsd = eurTry.Value / usdTry.Value;
                    outp.Add(New("EUR/KG(ALTIN)", usdKg / eurUsd, now, 0.0010m));
                    if (!Has("EUR/USD"))
                        outp.Add(New("EUR/USD", eurUsd, now, 0.0004m));
                }
            }

            // Çaprazlar
            if (!Has("USD/JPY") && usdTry is > 0 && jpyTry is > 0)
                outp.Add(New("USD/JPY", usdTry.Value / jpyTry.Value, now, 0.005m));
            if (!Has("EUR/USD") && eurTry is > 0 && usdTry is > 0)
                outp.Add(New("EUR/USD", eurTry.Value / usdTry.Value, now, 0.0004m));

            // Gümüş/Gram
            if (!Has("GÜMÜŞ/GRAM") && xagUsd is > 0 && usdTry is > 0)
            {
                var mid = (xagUsd.Value / OunceToGram) * usdTry.Value;
                outp.Add(New("GÜMÜŞ/GRAM", mid, now, 0.0020m));
            }

            return outp;

            static RateTick New(string sym, decimal mid, DateTime ts, decimal sp)
                => new(sym, mid * (1m - sp), mid * (1m + sp), mid, ts, "derived");
        }
    }
}

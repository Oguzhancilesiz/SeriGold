// Services/Concrete/DerivedRates.cs
using SeriGold.DTO.Rates;
using System.Collections.ObjectModel;

namespace SeriGold.Services.Concrete
{
    public static class DerivedRates
    {
        private const decimal OunceToGram = 31.1034768m;

        // Harem mantığı: sarrafiye = HAS GRAM * katsayı (alış = has ALIŞ * buyCoef, satış = has SATIŞ * sellCoef)
        private static readonly ReadOnlyDictionary<string, (string Title, decimal BuyCoef, decimal SellCoef, int Dp)>
        CoinCoef = new(new Dictionary<string, (string, decimal, decimal, int)>
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
        });

        private static readonly ReadOnlyDictionary<string, (string Title, decimal BidCoef, decimal AskCoef, int Dp)>
        KaratCoef = new(new Dictionary<string, (string, decimal, decimal, int)>
        {
            ["AYAR22/GRAM"] = ("AYAR22/GRAM", 0.9090m, 0.9530m, 2),
            ["AYAR14/GRAM"] = ("AYAR14/GRAM", 0.5480m, 0.7525m, 2),
        });

        public static IEnumerable<RateTick> Build(IReadOnlyList<RateTick> src)
        {
            var now = DateTime.UtcNow;
            var outp = new List<RateTick>();

            decimal? usdTry = Find(src, "USD/TRY")?.Mid;
            decimal? xauUsd = Find(src, "XAU/USD")?.Mid;
            decimal? xagUsd = Find(src, "XAG/USD")?.Mid;
            decimal? eurUsd = Find(src, "EUR/USD")?.Mid;

            if (usdTry is null || xauUsd is null) return outp;

            // HAS GRAM TL
            var gram24Mid = (xauUsd.Value / OunceToGram) * usdTry.Value;
            var gramHas = NewSp("ALTIN/GRAM", gram24Mid, now, 0.0010m);
            outp.Add(gramHas);

            // Ayarlar
            foreach (var kv in KaratCoef)
            {
                var meta = kv.Value;
                var bid = Math.Round(gramHas.Bid.GetValueOrDefault() * meta.BidCoef, meta.Dp);
                var ask = Math.Round(gramHas.Ask.GetValueOrDefault() * meta.AskCoef, meta.Dp);
                outp.Add(new RateTick(meta.Title, bid, ask, (bid + ask) / 2m, now, "derived"));
            }

            // KG/ALTIN
            var usdKg = xauUsd.Value * (1000m / OunceToGram);
            outp.Add(NewSp("USD/KG(ALTIN)", usdKg, now, 0.0010m));
            if (eurUsd is > 0) outp.Add(NewSp("EUR/KG(ALTIN)", usdKg / eurUsd.Value, now, 0.0010m));

            // Gümüş gram
            if (xagUsd is > 0)
            {
                var gramAgTry = (xagUsd.Value / OunceToGram) * usdTry.Value;
                outp.Add(NewSp("GÜMÜŞ/GRAM", gramAgTry, now, 0.0020m));
            }

            // Sarrafiye
            var hasBid = gramHas.Bid.GetValueOrDefault();
            var hasAsk = gramHas.Ask.GetValueOrDefault();
            foreach (var kv in CoinCoef)
            {
                var m = kv.Value;
                var bid = m.BuyCoef * hasBid;
                var ask = m.SellCoef * hasAsk;
                var mid = (bid + ask) / 2m;
                outp.Add(new RateTick(m.Title, bid, ask, mid, now, "derived"));
            }

            return outp;

            static RateTick? Find(IReadOnlyList<RateTick> s, string symbol) => s.FirstOrDefault(x => x.Symbol == symbol);
            static RateTick NewSp(string symbol, decimal mid, DateTime ts, decimal sp)
                => new(symbol, mid * (1m - sp), mid * (1m + sp), mid, ts, "derived");
        }
    }
}

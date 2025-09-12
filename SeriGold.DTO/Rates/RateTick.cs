using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// DTO/Rates/RateTick.cs
namespace SeriGold.DTO.Rates
{
    public sealed record RateTick(
        string Symbol,          // "EUR/TRY", "XAU/USD", "ALTIN/GRAM", ...
        decimal? Bid,           // alış
        decimal? Ask,           // satış
        decimal Mid,            // referans mid
        DateTime SourceTimeUtc, // sağlayıcının UTC zamanı
        string Source           // "binance", "erapi", "metals", "derived"
    );
}

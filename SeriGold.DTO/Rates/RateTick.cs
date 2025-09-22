using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DTO.Rates
{
    public sealed record RateTick(
        string Symbol,
        decimal? Bid,
        decimal? Ask,
        decimal Mid,
        DateTime SourceTimeUtc,
        string Source,
        decimal? Change = null  // sağlayıcıdan yüzde geliyorsa
    );
}

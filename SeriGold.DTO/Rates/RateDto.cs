using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DTO.Rates
{
    // Change nullable: yüzde bilgisi yoksa null
    public sealed record RateDto(string Symbol, decimal? Bid, decimal? Ask, decimal? Change, string Dir);
}



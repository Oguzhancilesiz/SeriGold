using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// DTO/Rates/RateDto.cs
namespace SeriGold.DTO.Rates
{
    public sealed record RateDto(
        string Symbol,
        decimal? Bid,
        decimal? Ask,
        decimal Change, // % (dünkü TR kapanışa göre)
        string Dir      // "up" | "down"
    );
}


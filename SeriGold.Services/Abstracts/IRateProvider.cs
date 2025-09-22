using SeriGold.DTO.Rates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Services.Abstracts
{
    public interface IRateProvider
    {
        Task<IReadOnlyList<RateTick>> GetAsync(CancellationToken ct = default);
    }
}


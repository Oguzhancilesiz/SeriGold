using SeriGold.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Services.Abstracts
{
    public interface IFaqService : IBaseService<FaqDto, FaqAddDto, FaqUpdateDto>
    {
        Task<(IReadOnlyList<FaqDto> Items, int Total)> Paged(int page, int size);
        Task ReorderAsync(IEnumerable<FaqOrderUpdateDto> items);
    }
}

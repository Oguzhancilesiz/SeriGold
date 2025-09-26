using SeriGold.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Services.Abstracts
{
    public interface IServiceItemService : IBaseService<ServiceItemDto, ServiceItemAddDto, ServiceItemUpdateDto>
    {
        Task<(IReadOnlyList<ServiceItemDto> Items, int Total)> Paged(int page, int size);
    }
}

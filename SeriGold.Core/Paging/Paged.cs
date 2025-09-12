using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Core.Paging
{
    public record PagedRequest(int Page = 1, int PageSize = 12);
    public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DTO
{
    public sealed class FaqOrderUpdateDto
    {
        public Guid Id { get; set; }
        public int DisplayOrder { get; set; }  // 1..N
    }
}

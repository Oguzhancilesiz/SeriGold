using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DTO
{
    public sealed class FaqDto
    {
        public Guid Id { get; set; }
        public int DisplayOrder { get; set; }
        public string Question { get; set; } = default!;
        public string AnswerHtml { get; set; } = default!;
        public bool IsActive { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

}

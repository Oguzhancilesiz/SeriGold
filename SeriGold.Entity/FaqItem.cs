using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Entity
{
    public sealed class FaqItem : BaseEntity
    {
        public int DisplayOrder { get; set; }
        public string Question { get; set; } = default!;
        public string AnswerHtml { get; set; } = default!;
        public bool IsActive { get; set; } = true;
    }
}

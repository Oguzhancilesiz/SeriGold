using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Entity
{
    public sealed class ServiceItem : BaseEntity
    {
        public int DisplayOrder { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Icon { get; set; } = "bi bi-activity"; // bootstrap ikon sınıfı
        public bool IsActive { get; set; } = true;
    }

}

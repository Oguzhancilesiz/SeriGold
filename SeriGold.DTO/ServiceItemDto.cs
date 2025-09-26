using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DTO
{
    public sealed class ServiceItemDto
    {
        public Guid Id { get; set; }
        public int DisplayOrder { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Icon { get; set; } = "bi bi-activity";
        public bool IsActive { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    public class ServiceItemAddDto
    {
        public int DisplayOrder { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Icon { get; set; } = "bi bi-activity";
        public bool IsActive { get; set; } = true;
    }
    public sealed class ServiceItemUpdateDto : ServiceItemAddDto
    {
        public Guid Id { get; set; }
    }
}

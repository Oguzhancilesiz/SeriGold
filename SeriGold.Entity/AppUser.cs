using Microsoft.AspNetCore.Identity;
using SeriGold.Core.Abstracts;
using SeriGold.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Entity
{
    public class AppUser : IdentityUser<Guid>, IEntity
    {
        public Status Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int AutoID { get; set; }
    }
}

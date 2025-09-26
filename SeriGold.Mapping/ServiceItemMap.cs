using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeriGold.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Mapping
{
    public sealed class ServiceItemMap : BaseMap<ServiceItem>
    {
        public override void Configure(EntityTypeBuilder<ServiceItem> b)
        {
            base.Configure(b);
            b.Property(x => x.Title).HasMaxLength(160);
            b.Property(x => x.Icon).HasMaxLength(80);
        }
    }

}

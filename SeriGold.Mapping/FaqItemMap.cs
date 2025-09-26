using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeriGold.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Mapping
{
    public sealed class FaqItemMap : BaseMap<FaqItem>
    {
        public override void Configure(EntityTypeBuilder<FaqItem> b)
        {
            base.Configure(b);
            b.Property(x => x.Question).HasMaxLength(300);
        }
    }
}

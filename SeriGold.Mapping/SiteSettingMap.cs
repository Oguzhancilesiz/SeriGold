using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeriGold.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Mapping
{
    public sealed class SiteSettingMap : BaseMap<SiteSetting>
    {
        public override void Configure(EntityTypeBuilder<SiteSetting> b)
        {
            base.Configure(b);
            b.Property(x => x.BrandName).HasMaxLength(120);
            b.Property(x => x.LogoUrl).HasMaxLength(400);
            b.Property(x => x.MetaTitle).HasMaxLength(200);
            b.Property(x => x.ThemeColorHex).HasMaxLength(16);
            b.Property(x => x.Phone).HasMaxLength(40);
            b.Property(x => x.Email).HasMaxLength(120);
            b.Property(x => x.MapsUrl).HasMaxLength(600);
        }
    }
}

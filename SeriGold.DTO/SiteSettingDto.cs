using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.DTO
{
    public class SiteSettingDto
    {
        public Guid Id { get; set; }
        public string BrandName { get; set; } = default!;
        public string LogoUrl { get; set; }
        public string MetaTitle { get; set; } = default!;
        public string MetaDescription { get; set; } = default!;
        public string ThemeColorHex { get; set; } = "#0b1621";
        public bool ShowAbout { get; set; }
        public bool ShowContact { get; set; }
        public string Phone { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string MapsUrl { get; set; } = default!;
        public string FooterNoteHtml { get; set; } = default!;
        public string HeroTitle { get; set; } = default!;
        public string HeroLead { get; set; } = default!;
        public string PromoTitle { get; set; } = default!;
        public string PromoText { get; set; } = default!;
        public string PrivacyHtml { get; set; } = default!;
        public string KvkkHtml { get; set; } = default!;
        public string AydinlatmaHtml { get; set; } = default!;
        public string CookieHtml { get; set; } = default!;
        public string DefaultConverterPair { get; set; } = "EUR/TRY";
        public string DefaultFxTab { get; set; } = "TRY";
        public DateTime? ModifiedDate { get; set; }
    }

    public class SiteSettingUpdateDto : SiteSettingDto

    {

    }
}

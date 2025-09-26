using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Entity
{
    public sealed class SiteSetting : BaseEntity
    {
        // Brand / Meta
        public string BrandName { get; set; } = "Seri Döviz";
        public string? LogoUrl { get; set; } = "/logo.png";
        public string MetaTitle { get; set; } = "Seri Döviz — Güvenilir Döviz & Sarrafiye";
        public string MetaDescription { get; set; } = "Seri Döviz ile canlı döviz kurları, altın ve sarrafiye fiyatları.";
        public string ThemeColorHex { get; set; } = "#0b1621";

        // Header links
        public bool ShowAbout { get; set; } = true;
        public bool ShowContact { get; set; } = true;

        // Contact
        public string Phone { get; set; } = "+905342903605";
        public string Email { get; set; } = "info@seridoviz.com";
        public string Address { get; set; } = "19 Mayıs Mah., Halaskargazi Cd. No:212 B, Şişli / İstanbul";
        public string MapsUrl { get; set; } =
            "https://www.google.com/maps/search/?api=1&query=19%20May%C4%B1s%20Mahallesi%20Halaskargazi%20Caddesi%20No%3A%20212%20B%2C%20%C5%9Ei%C5%9Fli%2F%C4%B0stanbul";

        // Footer
        public string FooterNoteHtml { get; set; } =
            "Created by <a href=\"https://www.linkedin.com/in/o%C4%9Fuzhan-%C3%A7ilesiz\" rel=\"noopener\" class=\"text-gold text-decoration-none\">Oğuzhan Çilesiz</a>";

        // Hero & Promo
        public string HeroTitle { get; set; } = "Seri Döviz ile güvenilir kurlar ve şeffaf fiyatlar";
        public string HeroLead { get; set; } = "Canlı piyasaları takip edin, altın ve sarrafiye fiyatlarını karşılaştırın.";
        public string PromoTitle { get; set; } = "2020’den bugüne, 2025’te daha kapsamlı hizmet.";
        public string PromoText { get; set; } =
            "Yüzyıllardır süren “önce güven” kültürünü şeffaflık ve teknolojiyle birleştiriyoruz.";

        // Modals
        public string PrivacyHtml { get; set; } = "<p>Gizlilik metni burada yönetilir.</p>";
        public string KvkkHtml { get; set; } = "<p>KVKK politikası burada yönetilir.</p>";
        public string AydinlatmaHtml { get; set; } = "<p>Aydınlatma metni burada yönetilir.</p>";
        public string CookieHtml { get; set; } = "<p>Çerez politikası burada yönetilir.</p>";

        // Döviz sayfası default’ları
        public string DefaultConverterPair { get; set; } = "EUR/TRY";
        public string DefaultFxTab { get; set; } = "TRY";
    }
}

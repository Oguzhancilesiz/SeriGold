using SeriGold.DTO;

namespace SeriGold.WebUI.Areas.Admin.Models
{
    public sealed class SiteSettingEditVm : SiteSettingUpdateDto
    {
        // Yüklenen yeni logo dosyası (opsiyonel)
        public IFormFile? LogoFile { get; set; }

        // Mevcut logoyu temizlemek için bayrak
        public bool RemoveLogo { get; set; }
    }
}

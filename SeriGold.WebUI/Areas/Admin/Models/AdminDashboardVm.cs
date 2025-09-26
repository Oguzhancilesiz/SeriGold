namespace SeriGold.WebUI.Areas.Admin.Models
{
    public sealed class AdminDashboardVm
    {
        public string BrandName { get; set; } = "Seri Döviz";
        public DateTime? SettingsModified { get; set; }

        public int FaqCount { get; set; }
        public int ServiceCount { get; set; }

        public IReadOnlyList<SeriGold.DTO.ServiceItemDto> LatestServices { get; set; } = Array.Empty<SeriGold.DTO.ServiceItemDto>();
        public IReadOnlyList<SeriGold.DTO.FaqDto> LatestFaqs { get; set; } = Array.Empty<SeriGold.DTO.FaqDto>();
    }
}

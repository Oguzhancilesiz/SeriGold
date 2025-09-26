using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeriGold.Services.Abstracts;
using SeriGold.WebUI.Areas.Admin.Models;

namespace SeriGold.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public sealed class DashboardController : Controller
    {
        private readonly ISiteSettingService _settings;
        private readonly IFaqService _faq;
        private readonly IServiceItemService _services;

        public DashboardController(ISiteSettingService settings, IFaqService faq, IServiceItemService services)
        {
            _settings = settings;
            _faq = faq;
            _services = services;
        }

        public async Task<IActionResult> Index()
        {
            var s = await _settings.GetAsync();
            var faqs = await _faq.GetAll();
            var srvs = await _services.GetAll();

            var vm = new AdminDashboardVm
            {
                BrandName = s.BrandName ?? "Seri Döviz",
                SettingsModified = s.ModifiedDate,
                FaqCount = faqs?.Count ?? 0,
                ServiceCount = srvs?.Count ?? 0,
                LatestServices = (srvs ?? new()).OrderByDescending(x => x.ModifiedDate).ThenBy(x => x.DisplayOrder).Take(5).ToList(),
                LatestFaqs = (faqs ?? new()).OrderByDescending(x => x.ModifiedDate).Take(5).ToList()
            };

            return View(vm);
        }
    }
}

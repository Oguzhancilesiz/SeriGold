using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeriGold.DTO;
using SeriGold.Services.Abstracts;
using SeriGold.WebUI.Areas.Admin.Models;

namespace SeriGold.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public sealed class SettingsController : Controller
    {
        private readonly ISiteSettingService _svc;
        private readonly IWebHostEnvironment _env;
        private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
        private static readonly HashSet<string> AllowedMime = new(StringComparer.OrdinalIgnoreCase)
        { "image/png","image/jpeg","image/webp","image/svg+xml" };

        public SettingsController(ISiteSettingService svc, IWebHostEnvironment env)
        {
            _svc = svc;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var s = await _svc.GetAsync();                // SiteSettingDto
            var vm = s.Adapt<SiteSettingEditVm>();        // ViewModel’e çevir
            return View(vm);                               // View: SiteSettingEditVm
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SiteSettingEditVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Güncel ayarları çek (mevcut LogoUrl’i koruyabilmek için)
            var current = await _svc.GetAsync();
            string? oldLogoUrl = current.LogoUrl;

            // 1) Logo Silme
            if (vm.RemoveLogo)
            {
                // upload klasörümüzdeki eski dosyayı silelim (site köküne gömülü default /logo.png’ye dokunma)
                if (!string.IsNullOrWhiteSpace(oldLogoUrl) && oldLogoUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    TryDeletePhysical(oldLogoUrl);
                }
                vm.LogoUrl = null;
            }

            // 2) Logo Yükleme
            if (vm.LogoFile is not null && vm.LogoFile.Length > 0)
            {
                if (!IsImage(vm.LogoFile))
                {
                    ModelState.AddModelError(nameof(vm.LogoFile), "Sadece PNG / JPG / WEBP / SVG yükleyin.");
                    return View(vm);
                }

                // Klasör: wwwroot/uploads/brand
                var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "brand");
                Directory.CreateDirectory(uploadsDir);

                var ext = Path.GetExtension(vm.LogoFile.FileName);
                var safeName = $"logo_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}".ToLowerInvariant();
                var physicalPath = Path.Combine(uploadsDir, safeName);

                using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await vm.LogoFile.CopyToAsync(fs);
                }

                // Eski upload’lı logo varsa temizle (default /logo.png ise elleme)
                if (!string.IsNullOrWhiteSpace(oldLogoUrl) && oldLogoUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    TryDeletePhysical(oldLogoUrl);
                }

                vm.LogoUrl = $"/uploads/brand/{safeName}";
            }

            // 3) Ne silindi ne de yeni yüklendi ise, mevcut değeri koru
            if (!vm.RemoveLogo && (vm.LogoFile is null || vm.LogoFile.Length == 0))
            {
                vm.LogoUrl = oldLogoUrl;
            }

            // Servise update ettir
            var dto = vm.Adapt<SiteSettingUpdateDto>();
            await _svc.UpdateAsync(dto);

            TempData["ok"] = "Kaydedildi.";
            return RedirectToAction(nameof(Edit));
        }

        private static bool IsImage(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExt.Contains(ext)) return false;
            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedMime.Contains(file.ContentType)) return false;
            return true;
        }

        private void TryDeletePhysical(string urlPath)
        {
            try
            {
                // /uploads/brand/.. → fiziksel yol
                var rooted = urlPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                var physical = Path.Combine(_env.WebRootPath ?? "wwwroot", rooted);
                if (System.IO.File.Exists(physical))
                    System.IO.File.Delete(physical);
            }
            catch { /* sessizce geç */ }
        }
    }
}

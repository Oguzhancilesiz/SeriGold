using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeriGold.DTO;
using SeriGold.Services.Abstracts;

namespace SeriGold.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public sealed class ServicesController : Controller
    {
        private readonly IServiceItemService _svc;
        public ServicesController(IServiceItemService svc) => _svc = svc;

        public async Task<IActionResult> Index() => View(await _svc.GetAll());

        [HttpGet] public IActionResult Create() => View(new ServiceItemAddDto());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceItemAddDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            await _svc.Add(dto);
            TempData["ok"] = "Hizmet eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var m = await _svc.GetById(id);
            return View(new ServiceItemUpdateDto
            {
                Id = m.Id,
                DisplayOrder = m.DisplayOrder,
                Title = m.Title,
                Description = m.Description,
                Icon = m.Icon,
                IsActive = m.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceItemUpdateDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            await _svc.Update(dto);
            TempData["ok"] = "Hizmet güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _svc.Delete(id);
            TempData["ok"] = "Hizmet silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}

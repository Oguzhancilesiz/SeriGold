// Areas/Admin/Controllers/FaqController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeriGold.DTO;
using SeriGold.Services.Abstracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SeriGold.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken] // <-- AJAX'ta token arar
    public sealed class FaqController : Controller
    {
        private readonly IFaqService _svc;
        public FaqController(IFaqService svc) => _svc = svc;

        public async Task<IActionResult> Index()
        {
            var items = await _svc.GetAll();
            return View(items);
        }

        [HttpGet] public IActionResult Create() => View(new FaqAddDto());
        [HttpPost] public async Task<IActionResult> Create(FaqAddDto dto) { if (!ModelState.IsValid) return View(dto); await _svc.Add(dto); TempData["ok"] = "Eklendi."; return RedirectToAction(nameof(Index)); }

        [HttpGet] public async Task<IActionResult> Edit(Guid id) { var m = await _svc.GetById(id); return View(new FaqUpdateDto { Id = m.Id, DisplayOrder = m.DisplayOrder, Question = m.Question, AnswerHtml = m.AnswerHtml, IsActive = m.IsActive }); }
        [HttpPost] public async Task<IActionResult> Edit(FaqUpdateDto dto) { if (!ModelState.IsValid) return View(dto); await _svc.Update(dto); TempData["ok"] = "Güncellendi."; return RedirectToAction(nameof(Index)); }

        [HttpPost] public async Task<IActionResult> Delete(Guid id) { await _svc.Delete(id); TempData["ok"] = "Silindi."; return RedirectToAction(nameof(Index)); }

        // YENİ: drag-drop sıralama kaydı
        [HttpPost]
        public async Task<IActionResult> Reorder([FromBody] List<FaqOrderUpdateDto> items)
        {
            if (items is null || items.Count == 0) return BadRequest();
            await _svc.ReorderAsync(items);
            return Ok(new { ok = true });
        }
    }
}

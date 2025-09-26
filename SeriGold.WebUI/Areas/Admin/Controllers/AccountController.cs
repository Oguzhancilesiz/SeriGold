using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SeriGold.Entity;
using SeriGold.WebUI.Areas.Admin.Models;

namespace SeriGold.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public sealed class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _sm;
        public AccountController(SignInManager<AppUser> sm) => _sm = sm;

        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginVm { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var res = await _sm.PasswordSignInAsync(
                vm.Email, vm.Password,
                isPersistent: vm.RememberMe,
                lockoutOnFailure: false);

            if (res.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                    return LocalRedirect(vm.ReturnUrl);

                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            ModelState.AddModelError(string.Empty, "Giriş başarısız. E-posta/şifreyi kontrol edin.");
            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _sm.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult Denied() => Content("Yetkisiz erişim.");
    }
}

using System.ComponentModel.DataAnnotations;

namespace SeriGold.WebUI.Areas.Admin.Models
{
    public sealed class LoginVm
    {
        [Required, EmailAddress, Display(Name = "E-posta")]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password), Display(Name = "Şifre")]
        public string Password { get; set; } = "";

        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

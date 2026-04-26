using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required(ErrorMessage = "Поле Email є обов'язковим")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Поле пароль обов'язкове")]
        [StringLength(100, ErrorMessage = "Пароль має містити щонайменше {2} і максимум {1} символів.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Паролі не співпадають.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}

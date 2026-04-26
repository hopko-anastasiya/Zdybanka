using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Поле Ім'я є обов'язковим")]
        [Display(Name = "Повне ім'я")]
        public string Fullname { get; set; } = null!;

        [Required(ErrorMessage = "Поле Email є обов'язковим")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email повинен містити символ '@' та крапку після нього")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Поле Пароль є обов'язковим")]
        [StringLength(100, ErrorMessage = "Пароль має містити щонайменше {2} символів.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$", ErrorMessage = "Пароль повинен містити мінімум одну велику літеру, одну малу літеру та одну цифру.")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Підтвердження пароля")]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
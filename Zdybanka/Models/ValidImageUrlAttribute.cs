using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models
{
    public class ValidImageUrlAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Якщо поле пусте, це OK (оскільки воно nullable)
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var url = value.ToString();

            // Перевіряємо, чи URL починається з http:// або https://
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Додатково перевіряємо, чи це валідний URL
                if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult("Посилання повинно починатися з http:// або https:// та бути валідним URL.");
        }
    }
}

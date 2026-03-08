using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models
{
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateTime)
            {
                if (dateTime <= DateTime.Now.AddHours(1))
                {
                    return new ValidationResult("Подія повинна бути запланована щонайменше за годину від теперішнього часу.");
                }
            }
            return ValidationResult.Success;
        }
    }
}

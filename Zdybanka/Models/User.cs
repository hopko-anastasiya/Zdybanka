using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Поле ім'я обов'язкове")]
    [Display(Name = "Повне ім'я")]
    public string Fullname { get; set; } = null!;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Поле Email є обов'язковим")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = null!;

    [Display(Name = "Дата створення")]
    public DateTime? Createdat { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public virtual ICollection<Userfavorite> Userfavorites { get; set; } = new List<Userfavorite>();
}

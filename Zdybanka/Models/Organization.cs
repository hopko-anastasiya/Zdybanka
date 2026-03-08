using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class Organization
{
    public int Id { get; set; }

    [Display(Name = "Статус")]
    public int? Statusid { get; set; }

    [Required(ErrorMessage = "Назва обов'язкова")]
    [Display(Name = "Назва")]
    public string Name { get; set; } = null!;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Поле Email є обов'язковим")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address.")]
    public string? Email { get; set; }

    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Display(Name = "Дата створення")]
    public DateTime? Createdat { get; set; }

    [Display(Name = "Дата оновлення")]
    public DateTime? Updatedat { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [Display(Name = "Статус")]
    public virtual Organizationstatus? Status { get; set; }
}

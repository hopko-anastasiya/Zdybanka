using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class Registration
{
    public int Id { get; set; }

    [Display(Name = "Користувач")]
    public int? Userid { get; set; }

    [Display(Name = "Подія")]
    public int? Eventid { get; set; }

    [Display(Name = "Дата реєстрації")]
    public DateTime? Registrationdate { get; set; }

    [Display(Name = "Подія")]
    public virtual Event? Event { get; set; }

    [Display(Name = "Користувач")]
    public virtual User? User { get; set; }
}

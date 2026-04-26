using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class Organization : User
{
    [Display(Name = "Статус")]
    public int? Statusid { get; set; }

    [Display(Name = "Опис")]
    public string? Description { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [Display(Name = "Статус")]
    public virtual Organizationstatus? Status { get; set; }
}

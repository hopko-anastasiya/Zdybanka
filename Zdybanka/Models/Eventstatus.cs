using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class Eventstatus
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Назва статусу обов'язкова")]
    [Display(Name = "Назва статусу")]
    public string Statusname { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

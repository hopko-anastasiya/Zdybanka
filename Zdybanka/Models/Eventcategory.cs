using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class Eventcategory
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Назва категорії обов'язкова")]
    [Display(Name = "Назва категорії")]
    public string Categoryname { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class Changeshistory
{
    public int Id { get; set; }

    public int? Eventid { get; set; }

    [Display(Name = "Дані змін")]
    public string? Changedata { get; set; }

    [Display(Name = "Дата зміни")]
    public DateTime? Changedat { get; set; }

    [Display(Name = "Подія")]
    public virtual Event? Event { get; set; }
}

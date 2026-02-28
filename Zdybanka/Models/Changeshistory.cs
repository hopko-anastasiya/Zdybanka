using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class Changeshistory
{
    public int Id { get; set; }

    public int? Eventid { get; set; }

    public string? Changedata { get; set; }

    public DateTime? Changedat { get; set; }

    public virtual Event? Event { get; set; }
}

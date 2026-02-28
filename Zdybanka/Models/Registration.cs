using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class Registration
{
    public int Id { get; set; }

    public int? Userid { get; set; }

    public int? Eventid { get; set; }

    public DateTime? Registrationdate { get; set; }

    public virtual Event? Event { get; set; }

    public virtual User? User { get; set; }
}

using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class Eventstatus
{
    public int Id { get; set; }

    public string Statusname { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

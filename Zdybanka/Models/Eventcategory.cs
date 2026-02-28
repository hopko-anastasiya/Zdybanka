using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class Eventcategory
{
    public int Id { get; set; }

    public string Categoryname { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

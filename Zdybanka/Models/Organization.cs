using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class Organization
{
    public int Id { get; set; }

    public int? Statusid { get; set; }

    public string Name { get; set; } = null!;

    public string? Email { get; set; }

    public string? Description { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual Organizationstatus? Status { get; set; }
}

using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class Organizationstatus
{
    public int Id { get; set; }

    public string Statusname { get; set; } = null!;

    public virtual ICollection<Organization> Organizations { get; set; } = new List<Organization>();
}

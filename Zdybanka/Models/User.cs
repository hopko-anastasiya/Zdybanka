using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class User
{
    public int Id { get; set; }

    public string Fullname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime? Createdat { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public virtual ICollection<Userfavorite> Userfavorites { get; set; } = new List<Userfavorite>();
}

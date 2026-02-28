using System;
using System.Collections.Generic;

namespace Zdybanka.Models;

public partial class Event
{
    public int Id { get; set; }

    public int? Organizationid { get; set; }

    public int? Categoryid { get; set; }

    public int? Statusid { get; set; }

    public string Title { get; set; } = null!;

    public string? Location { get; set; }

    public string? Description { get; set; }

    public DateTime? Eventdate { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public virtual Eventcategory? Category { get; set; }

    public virtual ICollection<Changeshistory> Changeshistories { get; set; } = new List<Changeshistory>();

    public virtual Organization? Organization { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public virtual Eventstatus? Status { get; set; }

    public virtual ICollection<Userfavorite> Userfavorites { get; set; } = new List<Userfavorite>();
}

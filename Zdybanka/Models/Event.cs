using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zdybanka.Models;

public partial class Event
{
    public int Id { get; set; }

    [Display(Name = "Організація")]
    public int? Organizationid { get; set; }

    [Display(Name = "Категорія")]
    public int? Categoryid { get; set; }

    [Display(Name = "Статус")]
    public int? Statusid { get; set; }

    [Required(ErrorMessage = "Назва обов'язкова")]
    [Display(Name = "Назва")]
    public string Title { get; set; } = null!;

    [Display(Name = "Місце проведення")]
    public string? Location { get; set; }

    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата обов'язкова")]
    [FutureDate]
    [Display(Name = "Дата та час події")]
    public DateTime? Eventdate { get; set; }

    [Display(Name = "Дата створення")]
    public DateTime? Createdat { get; set; }

    [Display(Name = "Дата оновлення")]
    public DateTime? Updatedat { get; set; }

    [Display(Name = "Категорія")]
    public virtual Eventcategory? Category { get; set; }

    public virtual ICollection<Changeshistory> Changeshistories { get; set; } = new List<Changeshistory>();

    [Display(Name = "Організація")]
    public virtual Organization? Organization { get; set; }

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    [Display(Name = "Статус")]
    public virtual Eventstatus? Status { get; set; }

    public virtual ICollection<Userfavorite> Userfavorites { get; set; } = new List<Userfavorite>();
}

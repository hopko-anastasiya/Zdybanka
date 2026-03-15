using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zdybanka.Models.ViewModels;

public class OrganizationEventsViewModel
{
    public Organization? Organization { get; set; }

    public List<Event> Events { get; set; } = new();

    public Dictionary<int, int> RegistrationCountsByEventId { get; set; } = new();

    public Event NewEvent { get; set; } = new();

    public List<SelectListItem> Categories { get; set; } = new();

    public List<SelectListItem> Statuses { get; set; } = new();

    public bool CanCreateEvent => string.Equals(Organization?.Status?.Statusname, "Верифікована", StringComparison.Ordinal);
}

namespace Zdybanka.Models.ViewModels;

public class OrganizationStatisticsViewModel
{
    public int OrganizationId { get; set; }

    public string OrganizationName { get; set; } = string.Empty;

    public int TotalEventsCount { get; set; }

    public int CompletedEventsCount { get; set; }

    public int TotalRegistrationsCount { get; set; }

    public int TotalFavoritesCount { get; set; }

    public List<PopularOrganizationEventViewModel> TopEventsByRegistrations { get; set; } = new();

    public List<PopularOrganizationEventViewModel> EventsEngagement { get; set; } = new();

    public List<EventStatusDistributionViewModel> EventsByStatus { get; set; } = new();
}

public class PopularOrganizationEventViewModel
{
    public int EventId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int RegistrationsCount { get; set; }

    public int FavoritesCount { get; set; }
}

public class EventStatusDistributionViewModel
{
    public string StatusName { get; set; } = string.Empty;

    public int EventsCount { get; set; }
}
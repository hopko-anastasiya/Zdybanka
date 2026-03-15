using Microsoft.EntityFrameworkCore;
using Zdybanka.Models;

namespace Zdybanka.Services;

public static class EventStatusAutomationService
{
    private static readonly TimeSpan EventDuration = TimeSpan.FromHours(1);

    public static async Task SynchronizeStatusesAsync(Lab1Context context, int? organizationId = null)
    {
        var statuses = await context.Eventstatuses
            .AsNoTracking()
            .Where(s => s.Statusname == "Запланована"
                || s.Statusname == "Заплановано"
                || s.Statusname == "Триває"
                || s.Statusname == "Проведена"
                || s.Statusname == "Відмінена")
            .ToListAsync();

        var plannedStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Запланована", StringComparison.Ordinal))
            ?? statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Заплановано", StringComparison.Ordinal));
        var ongoingStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Триває", StringComparison.Ordinal));
        var completedStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Проведена", StringComparison.Ordinal));
        var canceledStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Відмінена", StringComparison.Ordinal));

        if (plannedStatus == null || ongoingStatus == null || completedStatus == null || canceledStatus == null)
        {
            return;
        }

        var now = DateTime.Now;
        var eventsQuery = context.Events
            .Where(e => e.Eventdate.HasValue && e.Statusid != canceledStatus.Id);

        if (organizationId.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.Organizationid == organizationId.Value);
        }

        var eventsToUpdate = await eventsQuery.ToListAsync();
        var hasChanges = false;

        foreach (var item in eventsToUpdate)
        {
            var eventDate = item.Eventdate!.Value;
            var targetStatusId = eventDate > now
                ? plannedStatus.Id
                : eventDate.Add(EventDuration) > now
                    ? ongoingStatus.Id
                    : completedStatus.Id;

            if (item.Statusid == targetStatusId)
            {
                continue;
            }

            item.Statusid = targetStatusId;
            item.Updatedat = now;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync();
        }
    }
}
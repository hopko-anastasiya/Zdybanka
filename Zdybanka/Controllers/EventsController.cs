using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zdybanka.Models;
using Zdybanka.Models.ViewModels;
using Zdybanka.Services;

namespace Zdybanka.Controllers
{
    public class EventsController : Controller
    {
        private readonly Lab1Context _context;

        public EventsController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context);
            var lab1Context = _context.Events.Include(e => e.Category).Include(e => e.Organization).Include(e => e.Status);
            return View(await lab1Context.ToListAsync());
        }

        public async Task<IActionResult> MyEvents(int? organizationId = null)
        {
            var currentOrganizationId = organizationId ?? TemporaryIdentity.CurrentOrganizationId;
            var viewModel = await BuildOrganizationEventsViewModelAsync(currentOrganizationId);
            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMyEvent(int? organizationId, OrganizationEventsViewModel model)
        {
            var currentOrganizationId = organizationId ?? TemporaryIdentity.CurrentOrganizationId;
            var viewModel = await BuildOrganizationEventsViewModelAsync(currentOrganizationId, model.NewEvent);
            if (viewModel == null)
            {
                return NotFound();
            }

            if (viewModel.Organization == null)
            {
                return NotFound();
            }

            viewModel.NewEvent.Organizationid = currentOrganizationId;

            if (!CanOrganizationCreateEvents(viewModel.Organization))
            {
                ModelState.AddModelError(string.Empty, "Заблоковані або не верифіковані організації не можуть додавати нові події.");
            }

            if (ModelState.IsValid)
            {
                viewModel.NewEvent.Createdat = DateTime.Now;
                viewModel.NewEvent.Updatedat = DateTime.Now;
                _context.Events.Add(viewModel.NewEvent);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId });
            }

            ViewData["OpenCreateOverlay"] = true;
            return View("MyEvents", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMyEvent(
            int? organizationId,
            [Bind("Id,Categoryid,Statusid,Title,Location,Description,Eventdate")] Event eventData)
        {
            var currentOrganizationId = organizationId ?? TemporaryIdentity.CurrentOrganizationId;

            await EventStatusAutomationService.SynchronizeStatusesAsync(_context, currentOrganizationId);

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventData.Id && e.Organizationid == currentOrganizationId);

            if (existingEvent == null)
            {
                return NotFound();
            }

            // Дозволяємо редагування, але не раніше ніж за годину від поточного часу
            ModelState.Remove("Eventdate");
            if (eventData.Eventdate.HasValue && eventData.Eventdate.Value < DateTime.Now.AddHours(-1))
            {
                ModelState.AddModelError("Eventdate", "Дата події не може бути раніше ніж за годину від поточного часу.");
            }

            var editableStatuses = await GetOrganizationEditableStatusesAsync();
            var editableStatusIds = editableStatuses.Select(s => s.Id).ToHashSet();
            if (!eventData.Statusid.HasValue || (!editableStatusIds.Contains(eventData.Statusid.Value) && eventData.Statusid != existingEvent.Statusid))
            {
                ModelState.AddModelError("Statusid", "Організація може вручну змінювати статус лише на 'Запланована' або 'Відмінена'.");
            }

            if (ModelState.IsValid)
            {
                existingEvent.Title = eventData.Title;
                existingEvent.Location = eventData.Location;
                existingEvent.Description = eventData.Description;
                existingEvent.Eventdate = eventData.Eventdate;
                existingEvent.Categoryid = eventData.Categoryid;
                existingEvent.Statusid = eventData.Statusid;
                existingEvent.Updatedat = DateTime.Now;

                await RemoveRegistrationsIfEventCanceledAsync(existingEvent.Id, existingEvent.Statusid);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId });
            }

            var viewModel = await BuildOrganizationEventsViewModelAsync(currentOrganizationId);
            if (viewModel == null)
            {
                return NotFound();
            }

            ViewData["OpenEditOverlayForEventId"] = eventData.Id;
            return View("MyEvents", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMyEvent(int id, int? organizationId = null)
        {
            var currentOrganizationId = organizationId ?? TemporaryIdentity.CurrentOrganizationId;

            var _event = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.Organizationid == currentOrganizationId);

            if (_event != null)
            {
                _context.Events.Remove(_event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId });
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context);

            if (id == null)
            {
                return NotFound();
            }

            var _event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organization)
                .Include(e => e.Status)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (_event == null)
            {
                return NotFound();
            }

            return View(_event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Categoryname");

            var verifiedOrganizations = _context.Organizations.Include(o => o.Status).Where(o => o.Status.Statusname == "Верифікована");
            ViewData["Organizationid"] = new SelectList(verifiedOrganizations, "Id", "Name");
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname");
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Organizationid,Categoryid,Statusid,Title,Location,Description,Eventdate,Createdat,Updatedat")] Event _event)
        {
            var organization = await _context.Organizations.Include(o => o.Status).FirstOrDefaultAsync(o => o.Id == _event.Organizationid);

            if (organization == null || organization.Status.Statusname != "Верифікована")
            {
                ModelState.AddModelError("Organizationid", "Тільки верифіковані організації можуть створювати події.");
            }
            if (ModelState.IsValid)
            {
                _context.Add(_event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Categoryname", _event.Categoryid);
            var verifiedOrgs = _context.Organizations.Include(o => o.Status).Where(o => o.Status.Statusname == "Верифікована");
            ViewData["Organizationid"] = new SelectList(verifiedOrgs, "Id", "Name", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname", _event.Statusid);
            return View(_event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context);

            if (id == null)
            {
                return NotFound();
            }

            var _event = await _context.Events.FindAsync(id);
            if (_event == null)
            {
                return NotFound();
            }
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Categoryname", _event.Categoryid);
            var verifiedOrgs = _context.Organizations.Include(o => o.Status).Where(o => o.Status.Statusname == "Верифікована");
            ViewData["Organizationid"] = new SelectList(verifiedOrgs, "Id", "Name", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname", _event.Statusid);
            return View(_event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Organizationid,Categoryid,Statusid,Title,Location,Description,Eventdate")] Event _event)
        {
            if (id != _event.Id)
            {
                return NotFound();
            }

            var organization = await _context.Organizations.Include(o => o.Status).FirstOrDefaultAsync(o => o.Id == _event.Organizationid);
            if (organization == null || organization.Status.Statusname != "Верифікована")
            {
                ModelState.AddModelError("Organizationid", "Неможливо зберегти зміни: організація не верифікована.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEvent = await _context.Events.FirstOrDefaultAsync(o => o.Id == id);
                    if (existingEvent == null)
                    {
                        return NotFound();
                    }

                    existingEvent.Organizationid = _event.Organizationid;
                    existingEvent.Categoryid = _event.Categoryid;
                    existingEvent.Statusid = _event.Statusid;
                    existingEvent.Title = _event.Title;
                    existingEvent.Location = _event.Location;
                    existingEvent.Description = _event.Description;
                    existingEvent.Eventdate = _event.Eventdate;
                    existingEvent.Updatedat = DateTime.Now;

                    await RemoveRegistrationsIfEventCanceledAsync(existingEvent.Id, existingEvent.Statusid);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(_event.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Categoryname", _event.Categoryid);
            var verifiedOrgs = _context.Organizations.Include(o => o.Status).Where(o => o.Status.Statusname == "Верифікована");
            ViewData["Organizationid"] = new SelectList(verifiedOrgs, "Id", "Name", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname", _event.Statusid);
            return View(_event);
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context);

            if (id == null)
            {
                return NotFound();
            }

            var _event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organization)
                .Include(e => e.Status)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (_event == null)
            {
                return NotFound();
            }

            return View(_event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var _event = await _context.Events.FindAsync(id);
            if (_event != null)
            {
                _context.Events.Remove(_event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportRegistrations(int id)
        {
            var currentOrganizationId = TemporaryIdentity.CurrentOrganizationId;
            var targetEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.Organizationid == currentOrganizationId);

            if (targetEvent == null)
            {
                return NotFound();
            }

            var registrations = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.Eventid == id)
                .Include(r => r.User)
                .OrderByDescending(r => r.Registrationdate)
                .ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Користувач;Email;Дата реєстрації");

            foreach (var registration in registrations)
            {
                var fullName = registration.User?.Fullname ?? "Невідомий користувач";
                var email = registration.User?.Email ?? string.Empty;
                var registrationDate = registration.Registrationdate?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty;

                csvBuilder.AppendLine(
                    $"{EscapeCsv(fullName)};{EscapeCsv(email)};{EscapeCsv(registrationDate)}");
            }

            var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvBuilder.ToString())).ToArray();
            var safeTitle = string.IsNullOrWhiteSpace(targetEvent.Title) ? "event" : targetEvent.Title.Trim();
            var sanitizedTitle = new string(safeTitle
                .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)
                .ToArray());
            if (string.IsNullOrWhiteSpace(sanitizedTitle))
            {
                sanitizedTitle = "event";
            }

            var fileName = $"registrations_{sanitizedTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        private async Task<OrganizationEventsViewModel?> BuildOrganizationEventsViewModelAsync(int organizationId, Event? draftEvent = null)
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context, organizationId);

            var organization = await _context.Organizations
                .Include(o => o.Status)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return null;
            }

            var events = await _context.Events
                .Where(e => e.Organizationid == organizationId)
                .Include(e => e.Category)
                .Include(e => e.Status)
                .ToListAsync();

            events = events
                .OrderBy(e => e.Status?.Statusname switch
                {
                    "Запланована" => 0,
                    "Заплановано" => 0,
                    "Триває" => 1,
                    "Проведена" => 2,
                    "Відмінена" => 3,
                    _ => 4
                })
                .ThenByDescending(e => e.Eventdate)
                .ToList();

            var eventIds = events.Select(e => e.Id).ToList();
            var registrationCountsByEventId = await _context.Registrations
                .Where(r => r.Eventid.HasValue && eventIds.Contains(r.Eventid.Value))
                .GroupBy(r => r.Eventid!.Value)
                .ToDictionaryAsync(group => group.Key, group => group.Count());

            var categories = await _context.Eventcategories
                .OrderBy(c => c.Categoryname)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Categoryname
                })
                .ToListAsync();

            var statuses = (await GetOrganizationEditableStatusesAsync())
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Statusname
                })
                .ToList();

            var newEvent = draftEvent ?? new Event
            {
                Organizationid = organizationId
            };

            return new OrganizationEventsViewModel
            {
                Organization = organization,
                Events = events,
                RegistrationCountsByEventId = registrationCountsByEventId,
                NewEvent = newEvent,
                Categories = categories,
                Statuses = statuses
            };
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private async Task RemoveRegistrationsIfEventCanceledAsync(int eventId, int? statusId)
        {
            if (!statusId.HasValue)
            {
                return;
            }

            var statusName = await _context.Eventstatuses
                .Where(s => s.Id == statusId.Value)
                .Select(s => s.Statusname)
                .FirstOrDefaultAsync();

            if (!string.Equals(statusName, "Відмінена", StringComparison.Ordinal))
            {
                return;
            }

            var registrations = await _context.Registrations
                .Where(r => r.Eventid == eventId)
                .ToListAsync();

            if (registrations.Count == 0)
            {
                return;
            }

            _context.Registrations.RemoveRange(registrations);
        }

        private async Task<List<Eventstatus>> GetOrganizationEditableStatusesAsync()
        {
            var statuses = await _context.Eventstatuses
                .Where(s => s.Statusname == "Запланована"
                    || s.Statusname == "Заплановано"
                    || s.Statusname == "Відмінена")
                .ToListAsync();

            var editableStatuses = new List<Eventstatus>();
            var plannedStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Запланована", StringComparison.Ordinal))
                ?? statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Заплановано", StringComparison.Ordinal));
            var canceledStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Відмінена", StringComparison.Ordinal));

            if (plannedStatus != null)
            {
                editableStatuses.Add(plannedStatus);
            }

            if (canceledStatus != null)
            {
                editableStatuses.Add(canceledStatus);
            }

            return editableStatuses;
        }

        private static bool CanOrganizationCreateEvents(Organization organization)
        {
            return string.Equals(organization.Status?.Statusname, "Верифікована", StringComparison.Ordinal);
        }
    }
}

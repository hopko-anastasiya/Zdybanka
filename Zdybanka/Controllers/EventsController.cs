using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IAppAuthenticationService _appAuthenticationService;

        public EventsController(Lab1Context context, IAppAuthenticationService appAuthenticationService)
        {
            _context = context;
            _appAuthenticationService = appAuthenticationService;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context);

            IQueryable<Event> eventsQuery = _context.Events
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Organization)
                .Include(e => e.Status);

            eventsQuery = eventsQuery.Where(e => e.Status != null
                && e.Status.Statusname == "Запланована");

            var events = await eventsQuery
                .OrderBy(e => e.Eventdate)
                .ToListAsync();

            var eventIds = events.Select(e => e.Id).ToList();

            var registrationCounts = eventIds.Count == 0
                ? new Dictionary<int, int>()
                : await _context.Registrations
                    .AsNoTracking()
                    .Where(r => r.Eventid.HasValue && eventIds.Contains(r.Eventid.Value))
                    .GroupBy(r => r.Eventid!.Value)
                    .ToDictionaryAsync(group => group.Key, group => group.Count());

            var favoriteCounts = eventIds.Count == 0
                ? new Dictionary<int, int>()
                : await _context.Userfavorites
                    .AsNoTracking()
                    .Where(f => f.Eventid.HasValue && eventIds.Contains(f.Eventid.Value))
                    .GroupBy(f => f.Eventid!.Value)
                    .ToDictionaryAsync(group => group.Key, group => group.Count());

            ViewData["RegistrationCounts"] = registrationCounts;
            ViewData["FavoriteCounts"] = favoriteCounts;

            if (_appAuthenticationService.IsAuthenticated && _appAuthenticationService.CurrentUserId.HasValue)
            {
                var currentUserId = _appAuthenticationService.CurrentUserId.Value;
                var favoriteEventIds = await _context.Userfavorites
                    .AsNoTracking()
                    .Where(f => f.Userid == currentUserId && f.Eventid.HasValue && eventIds.Contains(f.Eventid.Value))
                    .Select(f => f.Eventid!.Value)
                    .ToListAsync();

                ViewData["FavoriteEventIds"] = favoriteEventIds;
            }

            return View(events);
        }

        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> MyEvents(int? organizationId = null)
        {
            if (organizationId.HasValue && organizationId.Value != _appAuthenticationService.CurrentUserId) return Forbid();
            var currentOrganizationId = organizationId ?? await ResolveCurrentOrganizationIdAsync();
            if (!currentOrganizationId.HasValue)
            {
                return NotFound();
            }

            var viewModel = await BuildOrganizationEventsViewModelAsync(currentOrganizationId.Value);
            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> SubmitOrganizationStatusRequest(int? organizationId = null)
        {
            if (organizationId.HasValue && organizationId.Value != _appAuthenticationService.CurrentUserId) return Forbid();
            var currentOrganizationId = organizationId ?? await ResolveCurrentOrganizationIdAsync();
            if (!currentOrganizationId.HasValue)
            {
                return NotFound();
            }

            var organization = await _context.Organizations
                .Include(o => o.Status)
                .FirstOrDefaultAsync(o => o.Id == currentOrganizationId.Value);

            if (organization == null)
            {
                return NotFound();
            }

            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var currentStatusName = organization.Status?.Statusname ?? string.Empty;
            var isBlocked = string.Equals(currentStatusName, "Заблокована", StringComparison.OrdinalIgnoreCase);
            var isPendingVerification = string.Equals(currentStatusName, "Очікує верифікації", StringComparison.OrdinalIgnoreCase);
            var isPendingUnblock = string.Equals(currentStatusName, "Очікує розблокування", StringComparison.OrdinalIgnoreCase);

            if (isPendingVerification || isPendingUnblock)
            {
                return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId.Value });
            }

            var targetStatusName = isBlocked ? "Очікує розблокування" : "Очікує верифікації";
            var targetStatus = statuses.FirstOrDefault(s =>
                string.Equals(s.Statusname, targetStatusName, StringComparison.OrdinalIgnoreCase));

            if (targetStatus == null)
            {
                return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId.Value });
            }

            organization.Statusid = targetStatus.Id;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId.Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> CreateMyEvent(int? organizationId, OrganizationEventsViewModel model)
        {
            if (organizationId.HasValue && organizationId.Value != _appAuthenticationService.CurrentUserId) return Forbid();
            var currentOrganizationId = organizationId ?? await ResolveCurrentOrganizationIdAsync();
            if (!currentOrganizationId.HasValue)
            {
                return NotFound();
            }

            var viewModel = await BuildOrganizationEventsViewModelAsync(currentOrganizationId.Value, model.NewEvent);
            if (viewModel == null)
            {
                return NotFound();
            }

            if (viewModel.Organization == null)
            {
                return NotFound();
            }

            viewModel.NewEvent.Organizationid = currentOrganizationId.Value;
            viewModel.NewEvent.ImageUrl = NormalizeNullableInput(viewModel.NewEvent.ImageUrl);

            if (!IsValidImageUrl(viewModel.NewEvent.ImageUrl))
            {
                ModelState.AddModelError("NewEvent.ImageUrl", "Посилання повинно починатися з http:// або https://.");
            }

            var plannedStatusId = await GetPlannedStatusIdAsync();
            if (!plannedStatusId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Не знайдено статус 'Запланована'. Зверніться до адміністратора.");
            }
            else
            {
                viewModel.NewEvent.Statusid = plannedStatusId.Value;
            }

            if (!CanOrganizationCreateEvents(viewModel.Organization))
            {
                ModelState.AddModelError(string.Empty, "Організація ще не верифікована адміністратором і не може створювати нові події.");
            }

            ValidateEventDateForCreation(viewModel.NewEvent.Eventdate, "NewEvent.Eventdate");

            if (ModelState.IsValid)
            {
                var now = DateTime.UtcNow;
                // �������� ���� �� UTC ���� ���� ��������� ��������
                if (viewModel.NewEvent.Eventdate.HasValue && viewModel.NewEvent.Eventdate.Value.Kind == DateTimeKind.Unspecified)
                {
                    viewModel.NewEvent.Eventdate = TimeZoneInfo.ConvertTimeToUtc(viewModel.NewEvent.Eventdate.Value, TimeZoneInfo.Local);
                }
                viewModel.NewEvent.Createdat = now;
                viewModel.NewEvent.Updatedat = now;
    
                _context.Events.Add(viewModel.NewEvent);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId.Value });
            }

            ViewData["OpenCreateOverlay"] = true;
            return View("MyEvents", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> ImportMyEvents(int? organizationId, IFormFile? csvFile)
        {
            if (organizationId.HasValue && organizationId.Value != _appAuthenticationService.CurrentUserId) return Forbid();
            var currentOrganizationId = organizationId ?? await ResolveCurrentOrganizationIdAsync();
            if (!currentOrganizationId.HasValue)
            {
                return NotFound();
            }

            var viewModel = await BuildOrganizationEventsViewModelAsync(currentOrganizationId.Value);
            if (viewModel == null)
            {
                return NotFound();
            }

            var result = await ImportEventsFromCsvAsync(currentOrganizationId.Value, csvFile, viewModel.Organization);

            ViewData["OpenCreateOverlay"] = true;
            ViewData["OpenCreateImportOverlay"] = true;
            ViewData["EventImportResult"] = result;

            return View("MyEvents", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> EditMyEvent(
            int? organizationId,
            [Bind("Id,Categoryid,Statusid,Title,Location,Description,Eventdate,ImageUrl")] Event eventData)
        {
            if (organizationId.HasValue && organizationId.Value != _appAuthenticationService.CurrentUserId) return Forbid();
            var currentOrganizationId = organizationId ?? await ResolveCurrentOrganizationIdAsync();
            if (!currentOrganizationId.HasValue)
            {
                return NotFound();
            }

            await EventStatusAutomationService.SynchronizeStatusesAsync(_context, currentOrganizationId.Value);

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventData.Id && e.Organizationid == currentOrganizationId.Value);

            if (existingEvent == null)
            {
                return NotFound();
            }

            ModelState.Remove("Eventdate");
            if (eventData.Eventdate.HasValue && eventData.Eventdate.Value < DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError("Eventdate", "Дата події повинна бути не раніше ніж за годину від поточного часу.");
            }

            eventData.ImageUrl = NormalizeNullableInput(eventData.ImageUrl);
            if (!IsValidImageUrl(eventData.ImageUrl))
            {
                ModelState.AddModelError("ImageUrl", "Посилання повинно починатися з http:// або https://.");
            }

            var editableStatuses = await GetOrganizationEditableStatusesAsync();
            var editableStatusIds = editableStatuses.Select(s => s.Id).ToHashSet();
            if (!eventData.Statusid.HasValue || (!editableStatusIds.Contains(eventData.Statusid.Value) && eventData.Statusid != existingEvent.Statusid))
            {
                ModelState.AddModelError("Statusid", "Організатор може обрати тільки статус 'Запланована' або 'Відмінена'.");
            }

            if (ModelState.IsValid)
            {
    
                if (eventData.Eventdate.HasValue && eventData.Eventdate.Value.Kind == DateTimeKind.Unspecified)
                {
                    eventData.Eventdate = TimeZoneInfo.ConvertTimeToUtc(eventData.Eventdate.Value, TimeZoneInfo.Local);
                }
                existingEvent.Title = eventData.Title;
                existingEvent.Location = eventData.Location;
                existingEvent.Description = eventData.Description;
                existingEvent.Eventdate = eventData.Eventdate;
                existingEvent.Categoryid = eventData.Categoryid;
                existingEvent.Statusid = eventData.Statusid;
                                existingEvent.ImageUrl = eventData.ImageUrl;
                existingEvent.Updatedat = DateTime.UtcNow;

                await RemoveRegistrationsIfEventCanceledAsync(existingEvent.Id, existingEvent.Statusid);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId.Value });
            }

            var viewModel = await BuildOrganizationEventsViewModelAsync(currentOrganizationId.Value);
            if (viewModel == null)
            {
                return NotFound();
            }

            ViewData["OpenEditOverlayForEventId"] = eventData.Id;
            return View("MyEvents", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> DeleteMyEvent(int id, int? organizationId = null)
        {
            if (organizationId.HasValue && organizationId.Value != _appAuthenticationService.CurrentUserId) return Forbid();
            var currentOrganizationId = organizationId ?? await ResolveCurrentOrganizationIdAsync();
            if (!currentOrganizationId.HasValue)
            {
                return NotFound();
            }

            var _event = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.Organizationid == currentOrganizationId.Value);

            if (_event != null)
            {
                _context.Events.Remove(_event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyEvents), new { organizationId = currentOrganizationId.Value });
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

            if (_event.Organizationid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            var sameCategoryEvents = await _context.Events
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Organization)
                .Include(e => e.Status)
                .Where(e => e.Id != _event.Id && e.Categoryid == _event.Categoryid)
                .ToListAsync();

            var sameCategoryEventIds = sameCategoryEvents.Select(e => e.Id).ToList();

            var registrationCounts = sameCategoryEventIds.Count == 0
                ? new Dictionary<int, int>()
                : await _context.Registrations
                    .AsNoTracking()
                    .Where(r => r.Eventid.HasValue && sameCategoryEventIds.Contains(r.Eventid.Value))
                    .GroupBy(r => r.Eventid!.Value)
                    .ToDictionaryAsync(group => group.Key, group => group.Count());

            var favoriteCounts = sameCategoryEventIds.Count == 0
                ? new Dictionary<int, int>()
                : await _context.Userfavorites
                    .AsNoTracking()
                    .Where(f => f.Eventid.HasValue && sameCategoryEventIds.Contains(f.Eventid.Value))
                    .GroupBy(f => f.Eventid!.Value)
                    .ToDictionaryAsync(group => group.Key, group => group.Count());

            var suggestedEvents = sameCategoryEvents
                .OrderByDescending(e => (registrationCounts.TryGetValue(e.Id, out var registrations) ? registrations : 0)
                    + (favoriteCounts.TryGetValue(e.Id, out var favorites) ? favorites : 0))
                .ThenBy(e => e.Eventdate)
                .Take(5)
                .ToList();

            ViewData["SuggestedEvents"] = suggestedEvents;
            ViewData["SuggestedRegistrationCounts"] = registrationCounts;
            ViewData["SuggestedFavoriteCounts"] = favoriteCounts;

            ViewData["IsAuthenticated"] = _appAuthenticationService.IsAuthenticated;
            if (_appAuthenticationService.IsAuthenticated && _appAuthenticationService.CurrentUserId.HasValue)
            {
                var currentUserId = _appAuthenticationService.CurrentUserId.Value;
                
                var suggestedEventIdsList = suggestedEvents.Select(e => e.Id).ToList();
                var suggestedFavoriteEventIds = await _context.Userfavorites
                    .AsNoTracking()
                    .Where(f => f.Userid == currentUserId && f.Eventid.HasValue && suggestedEventIdsList.Contains(f.Eventid.Value))
                    .Select(f => f.Eventid!.Value)
                    .ToListAsync();
                
                ViewData["SuggestedFavoriteEventIds"] = suggestedFavoriteEventIds;

                var isRegistered = await _context.Registrations
                    .AnyAsync(r => r.Userid == currentUserId && r.Eventid == _event.Id);
                var isFavorited = await _context.Userfavorites
                    .AnyAsync(f => f.Userid == currentUserId && f.Eventid == _event.Id);
                
                ViewData["IsRegistered"] = isRegistered;
                ViewData["IsFavorited"] = isFavorited;
            }

            return View(_event);
        }

        // GET: Events/Create
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public IActionResult Create()
        {
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Categoryname");

            var verifiedOrganizations = _context.Organizations.Include(o => o.Status).Where(o => o.Status.Statusname == "Верифікована");
            ViewData["Organizationid"] = new SelectList(verifiedOrganizations, "Id", "Fullname");
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname");
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> Create([Bind("Id,Organizationid,Categoryid,Statusid,Title,Location,Description,Eventdate,Createdat,ImageUrl")] Event _event)
        {
            var organization = await _context.Organizations.Include(o => o.Status).FirstOrDefaultAsync(o => o.Id == _event.Organizationid);

            if (organization == null || organization.Status.Statusname != "Верифікована")
            {
                ModelState.AddModelError("Organizationid", "Тільки верифіковані організації можуть створювати події.");
            }

            _event.ImageUrl = NormalizeNullableInput(_event.ImageUrl);
            if (!IsValidImageUrl(_event.ImageUrl))
            {
                ModelState.AddModelError("ImageUrl", "Посилання повинно починатися з http:// або https://.");
            }

            ValidateEventDateForCreation(_event.Eventdate, "Eventdate");

            if (ModelState.IsValid)
            {
                var now = DateTime.UtcNow;
                // �������� ���� �� UTC ���� ���� ��������� ��������
                if (_event.Eventdate.HasValue && _event.Eventdate.Value.Kind == DateTimeKind.Unspecified)
                {
                    _event.Eventdate = TimeZoneInfo.ConvertTimeToUtc(_event.Eventdate.Value, TimeZoneInfo.Local);
                }
                _event.Createdat = now;
                _event.Updatedat = now;
                _context.Add(_event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Categoryname", _event.Categoryid);
            var verifiedOrgs = _context.Organizations.Include(o => o.Status).Where(o => o.Status.Statusname == "Верифікована");
            ViewData["Organizationid"] = new SelectList(verifiedOrgs, "Id", "Fullname", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname", _event.Statusid);
            return View(_event);
        }

        // GET: Events/Edit/5
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
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

            if (_event.Organizationid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Categoryname", _event.Categoryid);
            var verifiedOrgs = _context.Organizations.Include(o => o.Status).Where(o => o.Status.Statusname == "Верифікована");
            ViewData["Organizationid"] = new SelectList(verifiedOrgs, "Id", "Fullname", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname", _event.Statusid);
            return View(_event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Organizationid,Categoryid,Statusid,Title,Location,Description,Eventdate,ImageUrl")] Event _event)
        {
            if (id != _event.Id)
            {
                return NotFound();
            }

            var organization = await _context.Organizations.Include(o => o.Status).FirstOrDefaultAsync(o => o.Id == _event.Organizationid);
            if (organization == null || organization.Status.Statusname != "Верифікована")
            {
                ModelState.AddModelError("Organizationid", "Тільки верифіковані організації можуть створювати події.");
            }

            _event.ImageUrl = NormalizeNullableInput(_event.ImageUrl);
            if (!IsValidImageUrl(_event.ImageUrl))
            {
                ModelState.AddModelError("ImageUrl", "Посилання повинно починатися з http:// або https://.");
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

                    // �������� ���� �� UTC ���� ���� ��������� ��������
                    if (_event.Eventdate.HasValue && _event.Eventdate.Value.Kind == DateTimeKind.Unspecified)
                    {
                        _event.Eventdate = TimeZoneInfo.ConvertTimeToUtc(_event.Eventdate.Value, TimeZoneInfo.Local);
                    }

                    existingEvent.Organizationid = _event.Organizationid;
                    existingEvent.Categoryid = _event.Categoryid;
                    existingEvent.Statusid = _event.Statusid;
                    existingEvent.Title = _event.Title;
                    existingEvent.Location = _event.Location;
                    existingEvent.Description = _event.Description;
                    existingEvent.Eventdate = _event.Eventdate;
                                        existingEvent.ImageUrl = _event.ImageUrl;
                    existingEvent.Updatedat = DateTime.UtcNow;

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
            ViewData["Organizationid"] = new SelectList(verifiedOrgs, "Id", "Fullname", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Statusname", _event.Statusid);
            return View(_event);
        }

        // GET: Events/Delete/5
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
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

            if (_event.Organizationid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            return View(_event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var _event = await _context.Events.FindAsync(id);
            if (_event != null)
            {
                if (_event.Organizationid != _appAuthenticationService.CurrentUserId) return Forbid();
                _context.Events.Remove(_event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportRegistrations(int id)
        {
            var currentOrganizationId = await ResolveCurrentOrganizationIdAsync();
            if (!currentOrganizationId.HasValue)
            {
                return NotFound();
            }

            var targetEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.Organizationid == currentOrganizationId.Value);

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
            csvBuilder.AppendLine("ПІБ;Email;Дата реєстрації");

            foreach (var registration in registrations)
            {
                var fullName = registration.User?.Fullname ?? "Не вказано";
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

        private async Task<int?> ResolveCurrentOrganizationIdAsync()
        {
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return null;
            }

            var organization = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == currentUserId.Value);
            return organization?.Id;
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
                    || s.Statusname == "Триває"
                    || s.Statusname == "Відмінена")
                .ToListAsync();

            var editableStatuses = new List<Eventstatus>();
            var plannedStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Запланована", StringComparison.Ordinal))
                ?? statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Триває", StringComparison.Ordinal));
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

        private async Task<int?> GetPlannedStatusIdAsync()
        {
            var statuses = await _context.Eventstatuses
                .AsNoTracking()
                .Where(s => s.Statusname == "Запланована" || s.Statusname == "Триває")
                .ToListAsync();

            var plannedStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Запланована", StringComparison.Ordinal))
                ?? statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Триває", StringComparison.Ordinal));

            return plannedStatus?.Id;
        }

        private void ValidateEventDateForCreation(DateTime? eventDate, string modelKey)
        {
            if (!eventDate.HasValue)
            {
                return;
            }

            if (eventDate.Value < DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError(modelKey, "Дата події не може бути раніше за 1 годину від поточної дати.");
            }
        }

        private static bool CanOrganizationCreateEvents(Organization organization)
        {
            return string.Equals(organization.Status?.Statusname, "Запланована", StringComparison.Ordinal);
        }

        private async Task<EventImportResultViewModel> ImportEventsFromCsvAsync(int organizationId, IFormFile? csvFile, Organization? organization)
        {
            var result = new EventImportResultViewModel();

            if (organization == null)
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "Організація не знайдена."
                });

                return result;
            }

            if (!CanOrganizationCreateEvents(organization))
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "Організація не може створювати події."
                });

                return result;
            }

            if (csvFile == null || csvFile.Length == 0)
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "Файл не вибрано або порожній."
                });

                return result;
            }

            var fileExtension = Path.GetExtension(csvFile.FileName);
            if (!string.Equals(fileExtension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "Файл не є CSV-файлом."
                });

                return result;
            }

            string fileContent;
            await using (var stream = csvFile.OpenReadStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                fileContent = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "CSV-файл порожній."
                });

                return result;
            }

            var delimiter = DetectCsvDelimiterForImport(fileContent);
            var records = ParseCsvRecordsForImport(fileContent, delimiter);

            if (records.Count == 0)
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "CSV-файл не містить даних."
                });

                return result;
            }

            var categories = await _context.Eventcategories
                .AsNoTracking()
                .ToListAsync();

            var categoryLookup = categories
                .Where(c => !string.IsNullOrWhiteSpace(c.Categoryname))
                .ToDictionary(c => NormalizeLookupValueForImport(c.Categoryname), c => c);

            var statuses = await _context.Eventstatuses
                .AsNoTracking()
                .ToListAsync();

            var defaultStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Запланована", StringComparison.OrdinalIgnoreCase))
                ?? statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Триває", StringComparison.OrdinalIgnoreCase));

            var statusLookup = statuses
                .Where(s => !string.IsNullOrWhiteSpace(s.Statusname))
                .ToDictionary(s => NormalizeLookupValueForImport(s.Statusname), s => s);

            var existingEvents = await _context.Events
                .AsNoTracking()
                .Where(e => e.Organizationid == organizationId)
                .Select(e => new { e.Title, e.Location, e.Eventdate })
                .ToListAsync();

            var existingEventKeys = new HashSet<string>(existingEvents
                .Select(e => BuildEventDuplicateKey(e.Title, e.Location, e.Eventdate))
                .Where(key => !string.IsNullOrWhiteSpace(key)));

            var seenEventKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var eventsToImport = new List<Event>();

            var hasHeaderRow = TryBuildEventHeaderMap(records[0], out var headerMap);
            var startIndex = hasHeaderRow ? 1 : 0;

            if (hasHeaderRow && !HasRequiredEventHeaderColumns(headerMap))
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 1,
                    IsSuccess = false,
                    Message = "CSV-файл не містить необхідних стовпців."
                });

                return result;
            }

            for (var recordIndex = startIndex; recordIndex < records.Count; recordIndex++)
            {
                var rowNumber = recordIndex + 1;
                var row = records[recordIndex];

                if (IsEmptyRowForImport(row))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = "����� �������� � ��� ����������."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                var title = GetCsvValueForImport(row, headerMap, hasHeaderRow, 0, "title", "назва", "name")?.Trim();
                var description = GetCsvValueForImport(row, headerMap, hasHeaderRow, 1, "description", "опис")?.Trim();
                var eventDateRaw = GetCsvValueForImport(row, headerMap, hasHeaderRow, 2, "eventdate", "дата", "date", "datetime")?.Trim();
                var location = GetCsvValueForImport(row, headerMap, hasHeaderRow, 3, "location", "місце", "place")?.Trim();
                var categoryName = GetCsvValueForImport(row, headerMap, hasHeaderRow, 4, "category", "категорія")?.Trim();
                var statusName = GetCsvValueForImport(row, headerMap, hasHeaderRow, 5, "status", "статус")?.Trim();
                var imageUrlRaw = GetCsvValueForImport(row, headerMap, hasHeaderRow, 6, "imageurl", "посилання на зображення", "зображення", "image")?.Trim();

                if (string.IsNullOrWhiteSpace(title))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = "�� ������� ����� ��䳿."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: �� ������� ����."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(eventDateRaw))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: �� ������� ����."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (!TryParseEventDateForImport(eventDateRaw, out var eventDate))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: ���� \"{eventDateRaw}\" �� ����������� ������."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                var imageUrl = string.IsNullOrWhiteSpace(imageUrlRaw) ? null : imageUrlRaw;
                if (!IsValidImageUrl(imageUrl))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: ���� ImageUrl ������� ���������� � http:// ��� https://."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[IMPORT DEBUG] Row {rowNumber}: Raw date string: '{eventDateRaw}', Parsed eventDate: {eventDate:O} (Kind: {eventDate.Kind}), UtcNow: {DateTime.UtcNow:O}");

                if (eventDate < DateTime.UtcNow.AddHours(1))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: ���� �� ���� �� ������ ��� �� ������ �� ��������� ����."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: �� ������� ���� ����������."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: �� ������� ��������."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                var normalizedCategory = NormalizeLookupValueForImport(categoryName);
                if (!categoryLookup.TryGetValue(normalizedCategory, out var selectedCategory))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: �������� \"{categoryName}\" �� ��������."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                Eventstatus? selectedStatus = defaultStatus;
                if (!string.IsNullOrWhiteSpace(statusName))
                {
                    var normalizedStatus = NormalizeLookupValueForImport(statusName);
                    if (!statusLookup.TryGetValue(normalizedStatus, out selectedStatus))
                    {
                        result.Messages.Add(new EventImportMessageViewModel
                        {
                            RowNumber = rowNumber,
                            IsSuccess = false,
                            Message = $"���� \"{title}\" �� ������: ������ \"{statusName}\" �� ��������."
                        });

                        result.SkippedCount += 1;
                        continue;
                    }
                }

                var duplicateKey = BuildEventDuplicateKey(title, location, eventDate);
                if (existingEventKeys.Contains(duplicateKey) || seenEventKeys.Contains(duplicateKey))
                {
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"���� \"{title}\" �� ������: ���� ���� ��� ����."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                var newEvent = new Event
                {
                    Organizationid = organizationId,
                    Categoryid = selectedCategory.Id,
                    Statusid = selectedStatus?.Id,
                    Title = title,
                    Description = description,
                    Eventdate = eventDate,
                    Location = location,
                                        ImageUrl = imageUrl,
                    Createdat = DateTime.UtcNow,
                    Updatedat = DateTime.UtcNow
                };

                eventsToImport.Add(newEvent);
                existingEventKeys.Add(duplicateKey);
                seenEventKeys.Add(duplicateKey);
                result.ImportedCount += 1;
            }

            if (eventsToImport.Count > 0)
            {
                foreach (var eventItem in eventsToImport)
                {
                    var eventDateStr = eventItem.Eventdate.HasValue ? $"{eventItem.Eventdate.Value:O} (Kind: {eventItem.Eventdate.Value.Kind})" : "null";
                    System.Diagnostics.Debug.WriteLine($"[IMPORT DEBUG] Saving event: {eventItem.Title}, Eventdate: {eventDateStr}");
                    _context.Events.Add(eventItem);
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    var dbError = ex.InnerException?.Message ?? ex.Message;
                    result.Messages.Clear();
                    result.Messages.Add(new EventImportMessageViewModel
                    {
                        RowNumber = 0,
                        IsSuccess = false,
                        Message = $"������� ��� ���������� ���� �� ���� �����: {dbError}"
                    });
                    result.ImportedCount = 0;
                }
            }

            if (result.Messages.Count == 0 && eventsToImport.Count == 0)
            {
                result.Messages.Add(new EventImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "CSV-���� �� ������ ����� ��� �������."
                });
            }

            return result;
        }

        private static bool TryBuildEventHeaderMap(IReadOnlyList<string> firstRow, out Dictionary<string, int> headerMap)
        {
            headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var hasKnownHeader = false;

            for (var index = 0; index < firstRow.Count; index++)
            {
                var normalizedHeader = NormalizeLookupValueForImport(firstRow[index]);
                if (string.IsNullOrWhiteSpace(normalizedHeader))
                {
                    continue;
                }

                if (EventHeaderAliases.Contains(normalizedHeader))
                {
                    headerMap[normalizedHeader] = index;
                    hasKnownHeader = true;
                }
            }

            return hasKnownHeader;
        }

        private static bool HasRequiredEventHeaderColumns(IReadOnlyDictionary<string, int> headerMap)
        {
            return HasEventHeaderColumn(headerMap, "title", "назва", "name")
                && HasEventHeaderColumn(headerMap, "description", "опис")
                && HasEventHeaderColumn(headerMap, "eventdate", "дата", "date", "datetime")
                && HasEventHeaderColumn(headerMap, "location", "місце", "place")
                && HasEventHeaderColumn(headerMap, "category", "категорія");
        }

        private static bool HasEventHeaderColumn(IReadOnlyDictionary<string, int> headerMap, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                if (headerMap.ContainsKey(NormalizeLookupValueForImport(alias)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string? GetCsvValueForImport(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int>? headerMap,
            bool hasHeaderRow,
            int fallbackIndex,
            params string[] aliases)
        {
            if (hasHeaderRow && headerMap != null)
            {
                foreach (var alias in aliases)
                {
                    var normalizedAlias = NormalizeLookupValueForImport(alias);
                    if (headerMap.TryGetValue(normalizedAlias, out var index) && index < row.Count)
                    {
                        return row[index];
                    }
                }

                return null;
            }

            return fallbackIndex < row.Count ? row[fallbackIndex] : null;
        }

        private static bool TryParseEventDateForImport(string value, out DateTime parsedDate)
        {
            var formats = new[]
            {
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-ddTHH:mm",
                "dd.MM.yyyy HH:mm",
                "dd/MM/yyyy HH:mm",
                "yyyy-MM-dd"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    // Convert from local time to UTC
                    if (parsedDate.Kind == DateTimeKind.Unspecified)
                    {
                        parsedDate = TimeZoneInfo.ConvertTimeToUtc(parsedDate, TimeZoneInfo.Local);
                    }
                    return true;
                }
            }

            if (DateTime.TryParse(value, out parsedDate))
            {
                // Convert from local time to UTC
                if (parsedDate.Kind == DateTimeKind.Unspecified)
                {
                    parsedDate = TimeZoneInfo.ConvertTimeToUtc(parsedDate, TimeZoneInfo.Local);
                }
                return true;
            }

            return false;
        }

        private static bool IsEmptyRowForImport(IReadOnlyCollection<string> row)
        {
            return row.All(string.IsNullOrWhiteSpace);
        }

        private static char DetectCsvDelimiterForImport(string content)
        {
            var firstLine = content
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;

            var semicolonCount = firstLine.Count(character => character == ';');
            var commaCount = firstLine.Count(character => character == ',');

            return semicolonCount >= commaCount ? ';' : ',';
        }

        private static List<List<string>> ParseCsvRecordsForImport(string content, char delimiter)
        {
            var records = new List<List<string>>();
            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            var isInsideQuotes = false;

            for (var index = 0; index < content.Length; index++)
            {
                var character = content[index];

                if (isInsideQuotes)
                {
                    if (character == '"')
                    {
                        var nextIndex = index + 1;
                        if (nextIndex < content.Length && content[nextIndex] == '"')
                        {
                            currentCell.Append('"');
                            index = nextIndex;
                        }
                        else
                        {
                            isInsideQuotes = false;
                        }
                    }
                    else
                    {
                        currentCell.Append(character);
                    }

                    continue;
                }

                if (character == '"')
                {
                    isInsideQuotes = true;
                    continue;
                }

                if (character == delimiter)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    continue;
                }

                if (character == '\r')
                {
                    continue;
                }

                if (character == '\n')
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    records.Add(currentRow);
                    currentRow = new List<string>();
                    continue;
                }

                currentCell.Append(character);
            }

            currentRow.Add(currentCell.ToString());
            if (currentRow.Count > 1 || !string.IsNullOrWhiteSpace(currentRow[0]) || records.Count == 0)
            {
                records.Add(currentRow);
            }

            return records;
        }

        private static string NormalizeLookupValueForImport(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value.Trim())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            return builder.ToString();
        }

        private static string BuildEventDuplicateKey(string? title, string? location, DateTime? eventDate)
        {
            var normalizedTitle = NormalizeLookupValueForImport(title);
            var normalizedLocation = NormalizeLookupValueForImport(location);
            var normalizedDate = eventDate?.ToString("yyyyMMddHHmm") ?? string.Empty;

            return $"{normalizedTitle}|{normalizedDate}|{normalizedLocation}";
        }

        private static string? NormalizeNullableInput(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsValidImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return true;
            }

            return imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private static readonly HashSet<string> EventHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeLookupValueForImport("title"),
            NormalizeLookupValueForImport("�����"),
            NormalizeLookupValueForImport("name"),
            NormalizeLookupValueForImport("description"),
            NormalizeLookupValueForImport("����"),
            NormalizeLookupValueForImport("eventdate"),
            NormalizeLookupValueForImport("����"),
            NormalizeLookupValueForImport("date"),
            NormalizeLookupValueForImport("datetime"),
            NormalizeLookupValueForImport("location"),
            NormalizeLookupValueForImport("����"),
            NormalizeLookupValueForImport("place"),
            NormalizeLookupValueForImport("category"),
            NormalizeLookupValueForImport("��������"),
            NormalizeLookupValueForImport("status"),
            NormalizeLookupValueForImport("������"),
            NormalizeLookupValueForImport("imageurl"),
            NormalizeLookupValueForImport("image"),
            NormalizeLookupValueForImport("��������� �� ����������"),
            NormalizeLookupValueForImport("���������")
        };
    }
}





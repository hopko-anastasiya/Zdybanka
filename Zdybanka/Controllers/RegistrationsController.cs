using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zdybanka.Models;
using Zdybanka.Services;

namespace Zdybanka.Controllers
{
    [Authorize(Roles = nameof(UserRole.User))]
    public class RegistrationsController : Controller
    {
        private readonly Lab1Context _context;
        private readonly IAppAuthenticationService _appAuthenticationService;

        public RegistrationsController(Lab1Context context, IAppAuthenticationService appAuthenticationService)
        {
            _context = context;
            _appAuthenticationService = appAuthenticationService;
        }

        // GET: Registrations
        public async Task<IActionResult> Index()
        {
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            var events = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.Userid == currentUserId.Value && r.Event != null)
                .Include(r => r.Event)
                    .ThenInclude(e => e.Category)
                .Select(r => r.Event!)
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

            var favoriteEventIds = await _context.Userfavorites
                .AsNoTracking()
                .Where(f => f.Userid == currentUserId.Value && f.Eventid.HasValue && eventIds.Contains(f.Eventid.Value))
                .Select(f => f.Eventid!.Value)
                .ToListAsync();

            ViewData["IsAuthenticated"] = true;
            ViewData["RegistrationCounts"] = registrationCounts;
            ViewData["FavoriteCounts"] = favoriteCounts;
            ViewData["FavoriteEventIds"] = favoriteEventIds;

            return View(events);
        }

        // GET: Registrations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (registration == null)
            {
                return NotFound();
            }

            if (registration.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            return View(registration);
        }

        // GET: Registrations/Create
        public async Task<IActionResult> Create()
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context);
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            var allowedEvents = _context.Events.Include(e => e.Status)
                .Where(e => e.Status == null || (e.Status.Statusname != "���������" && e.Status.Statusname != "³������"));
            ViewData["Eventid"] = new SelectList(allowedEvents, "Id", "Title");
            ViewData["Userid"] = new SelectList(_context.Users.Where(u => u.Id == currentUserId.Value), "Id", "Fullname", currentUserId.Value);
            return View();
        }

        // POST: Registrations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Userid,Eventid,Registrationdate")] Registration registration)
        {
            await EventStatusAutomationService.SynchronizeStatusesAsync(_context);
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            registration.Userid = currentUserId.Value;

            var selectedEvent = await _context.Events.Include(e => e.Status).FirstOrDefaultAsync(e => e.Id == registration.Eventid);
            if (selectedEvent?.Status != null && (selectedEvent.Status.Statusname == "���������" || selectedEvent.Status.Statusname == "³������"))
            {
                ModelState.AddModelError("Eventid", "��������� �������������� �� ���� � �������� '" + selectedEvent.Status.Statusname + "'.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(registration);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var allowedEvents = _context.Events.Include(e => e.Status)
                .Where(e => e.Status == null || (e.Status.Statusname != "���������" && e.Status.Statusname != "³������"));
            ViewData["Eventid"] = new SelectList(allowedEvents, "Id", "Title", registration.Eventid);
            ViewData["Userid"] = new SelectList(_context.Users.Where(u => u.Id == currentUserId.Value), "Id", "Fullname", currentUserId.Value);
            return View(registration);
        }

        // GET: Registrations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (registration == null)
            {
                return NotFound();
            }

            if (registration.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            return View(registration);
        }

        // POST: Registrations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registration = await _context.Registrations.FindAsync(id);
            if (registration != null)
            {
                if (registration.Userid != _appAuthenticationService.CurrentUserId) return Forbid();
                _context.Registrations.Remove(registration);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RegistrationExists(int id)
        {
            return _context.Registrations.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int eventId, string? returnUrl = null)
        {
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(f => f.Userid == currentUserId.Value && f.Eventid == eventId);

            if (existingRegistration == null)
            {
                _context.Registrations.Add(new Registration
                {
                    Userid = currentUserId.Value,
                    Eventid = eventId,
                    Registrationdate = DateTime.Now // using current time for the registration
                });
            }
            else
            {
                _context.Registrations.Remove(existingRegistration);
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Events");
        }
    }
}





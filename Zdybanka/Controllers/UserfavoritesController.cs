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
    public class UserfavoritesController : Controller
    {
        private readonly Lab1Context _context;
        private readonly IAppAuthenticationService _appAuthenticationService;

        public UserfavoritesController(Lab1Context context, IAppAuthenticationService appAuthenticationService)
        {
            _context = context;
            _appAuthenticationService = appAuthenticationService;
        }

        // GET: Userfavorites
        public async Task<IActionResult> Index()
        {
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            var events = await _context.Userfavorites
                .AsNoTracking()
                .Where(u => u.Userid == currentUserId.Value && u.Event != null)
                .Include(u => u.Event)
                    .ThenInclude(e => e.Category)
                .Select(u => u.Event!)
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

        // GET: Userfavorites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userfavorite = await _context.Userfavorites
                .Include(u => u.Event)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userfavorite == null)
            {
                return NotFound();
            }

            if (userfavorite.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            if (userfavorite.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            return View(userfavorite);
        }

        // GET: Userfavorites/Create
        public IActionResult Create()
        {
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Title");
            ViewData["Userid"] = new SelectList(_context.Users.Where(u => u.Id == currentUserId.Value), "Id", "Fullname", currentUserId.Value);
            return View();
        }

        // POST: Userfavorites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Userid,Eventid")] Userfavorite userfavorite)
        {
            var currentUserId = _appAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            userfavorite.Userid = currentUserId.Value;

            if (ModelState.IsValid)
            {
                _context.Add(userfavorite);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Title", userfavorite.Eventid);
            ViewData["Userid"] = new SelectList(_context.Users.Where(u => u.Id == currentUserId.Value), "Id", "Fullname", currentUserId.Value);
            return View(userfavorite);
        }

        // GET: Userfavorites/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userfavorite = await _context.Userfavorites.FindAsync(id);
            if (userfavorite == null)
            {
                return NotFound();
            }

            if (userfavorite.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Title", userfavorite.Eventid);
            ViewData["Userid"] = new SelectList(_context.Users, "Id", "Fullname", userfavorite.Userid);
            return View(userfavorite);
        }

        // POST: Userfavorites/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Userid,Eventid")] Userfavorite userfavorite)
        {
            if (id != userfavorite.Id)
            {
                return NotFound();
            }

            if (userfavorite.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userfavorite);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserfavoriteExists(userfavorite.Id))
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
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Title", userfavorite.Eventid);
            ViewData["Userid"] = new SelectList(_context.Users, "Id", "Fullname", userfavorite.Userid);
            return View(userfavorite);
        }

        // GET: Userfavorites/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userfavorite = await _context.Userfavorites
                .Include(u => u.Event)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userfavorite == null)
            {
                return NotFound();
            }

            if (userfavorite.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            if (userfavorite.Userid != _appAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            return View(userfavorite);
        }

        // POST: Userfavorites/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userfavorite = await _context.Userfavorites.FindAsync(id);
            if (userfavorite != null)
            {
                if (userfavorite.Userid != _appAuthenticationService.CurrentUserId) return Forbid();
                _context.Userfavorites.Remove(userfavorite);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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

            var existingFavorite = await _context.Userfavorites
                .FirstOrDefaultAsync(f => f.Userid == currentUserId.Value && f.Eventid == eventId);

            if (existingFavorite == null)
            {
                _context.Userfavorites.Add(new Userfavorite
                {
                    Userid = currentUserId.Value,
                    Eventid = eventId
                });
            }
            else
            {
                _context.Userfavorites.Remove(existingFavorite);
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Events");
        }

        private bool UserfavoriteExists(int id)
        {
            return _context.Userfavorites.Any(e => e.Id == id);
        }
    }
}







using System;
using System.Collections.Generic;
using System.Linq;
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
    [Authorize(Roles = nameof(UserRole.OrganizationManager))]
    public class OrganizationsController : Controller
    {
        private readonly Lab1Context _context;
        private readonly IAppAuthenticationService _AppAuthenticationService;

        public OrganizationsController(Lab1Context context, IAppAuthenticationService AppAuthenticationService)
        {
            _context = context;
            _AppAuthenticationService = AppAuthenticationService;
        }

        // GET: Organizations
        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.Organizations.Include(o => o.Status);
            return View(await lab1Context.ToListAsync());
        }

        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> MyIndex()
        {
            var lab1Context = _context.Organizations.Include(o => o.Status);
            return View(await lab1Context.ToListAsync());
        }

        // GET: Organizations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organization = await _context.Organizations
                .Include(o => o.Status)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        // GET: Organizations/Create
        public IActionResult Create()
        {
            ViewData["Statusid"] = new SelectList(_context.Organizationstatuses, "Id", "Statusname");
            return View();
        }

        // POST: Organizations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Statusid,Fullname,Email,Description,Createdat")] Organization organization)
        {
            organization.PasswordHash = Guid.NewGuid().ToString("N");
            organization.Role = UserRole.OrganizationManager;
            ModelState.Remove(nameof(Organization.PasswordHash));

            if (ModelState.IsValid)
            {
                organization.Createdat = DateTime.Now;

                _context.Add(organization);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Statusid"] = new SelectList(_context.Organizationstatuses, "Id", "Statusname", organization.Statusid);
            return View(organization);
        }

        // GET: Organizations/Edit/5
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (id != _AppAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            var organization = await _context.Organizations.FindAsync(id);
            if (organization == null)
            {
                return NotFound();
            }
            ViewData["Statusid"] = new SelectList(_context.Organizationstatuses, "Id", "Statusname", organization.Statusid);
            return View(organization);
        }

        // POST: Organizations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Statusid,Fullname,Email,Description")] Organization organization, string? returnUrl = null)
        {
            if (id != organization.Id)
            {
                return NotFound();
            }

            if (id != _AppAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            ModelState.Remove(nameof(Organization.PasswordHash));

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrganization = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == id);
                    if (existingOrganization == null)
                    {
                        return NotFound();
                    }

                    existingOrganization.Statusid = organization.Statusid;
                    existingOrganization.Fullname = organization.Fullname;
                    existingOrganization.Email = organization.Email;
                    existingOrganization.Description = organization.Description;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrganizationExists(organization.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
            ViewData["Statusid"] = new SelectList(_context.Organizationstatuses, "Id", "Statusname", organization.Statusid);
            return View(organization);
        }

        // GET: Organizations/Delete/5
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (id != _AppAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            var organization = await _context.Organizations
                .Include(o => o.Status)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        // POST: Organizations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id != _AppAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }

            var organization = await _context.Organizations.FindAsync(id);
            if (organization != null)
            {
                _context.Organizations.Remove(organization);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrganizationExists(int id)
        {
            return _context.Organizations.Any(e => e.Id == id);
        }

        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> Profile(int? id = null)
        {
            if (id.HasValue && id != _AppAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }
            var organizationId = id ?? await ResolveCurrentOrganizationIdAsync();

            if (!organizationId.HasValue)
            {
                return NotFound();
            }

            var organization = await _context.Organizations.Include(o => o.Status).FirstOrDefaultAsync(m => m.Id == organizationId.Value);

            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        [Authorize(Roles = nameof(UserRole.OrganizationManager))]
        public async Task<IActionResult> Statistics(int? id = null)
        {
            if (id.HasValue && id != _AppAuthenticationService.CurrentUserId)
            {
                return Forbid();
            }
            var organizationId = id ?? await ResolveCurrentOrganizationIdAsync();

            if (!organizationId.HasValue)
            {
                return NotFound();
            }

            await EventStatusAutomationService.SynchronizeStatusesAsync(_context, organizationId.Value);

            var organization = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == organizationId.Value);

            if (organization == null)
            {
                return NotFound();
            }

            var eventsQuery = _context.Events
                .AsNoTracking()
                .Where(e => e.Organizationid == organizationId.Value);

            var totalEventsCount = await eventsQuery.CountAsync();

            var completedEventsCount = await eventsQuery
                .Where(e => e.Status != null && e.Status.Statusname == "���������")
                .CountAsync();

            var totalRegistrationsCount = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.Event != null && r.Event.Organizationid == organizationId.Value)
                .CountAsync();

            var totalFavoritesCount = await _context.Userfavorites
                .AsNoTracking()
                .Where(f => f.Event != null && f.Event.Organizationid == organizationId.Value)
                .CountAsync();

            var topEvents = await eventsQuery
                .Select(e => new PopularOrganizationEventViewModel
                {
                    EventId = e.Id,
                    Title = e.Title,
                    RegistrationsCount = e.Registrations.Count(),
                    FavoritesCount = e.Userfavorites.Count()
                })
                .OrderByDescending(e => e.RegistrationsCount)
                .ThenByDescending(e => e.FavoritesCount)
                .ThenBy(e => e.Title)
                .Take(3)
                .ToListAsync();

            var eventsEngagement = await eventsQuery
                .Select(e => new PopularOrganizationEventViewModel
                {
                    EventId = e.Id,
                    Title = e.Title,
                    RegistrationsCount = e.Registrations.Count(),
                    FavoritesCount = e.Userfavorites.Count()
                })
                .OrderBy(e => e.Title)
                .ToListAsync();

            var eventsByStatus = await eventsQuery
                .Select(e => e.Status != null ? e.Status.Statusname : "��� �������")
                .GroupBy(statusName => statusName)
                .Select(group => new EventStatusDistributionViewModel
                {
                    StatusName = group.Key,
                    EventsCount = group.Count()
                })
                .OrderByDescending(item => item.EventsCount)
                .ThenBy(item => item.StatusName)
                .ToListAsync();

            var viewModel = new OrganizationStatisticsViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Fullname,
                TotalEventsCount = totalEventsCount,
                CompletedEventsCount = completedEventsCount,
                TotalRegistrationsCount = totalRegistrationsCount,
                TotalFavoritesCount = totalFavoritesCount,
                TopEventsByRegistrations = topEvents,
                EventsEngagement = eventsEngagement,
                EventsByStatus = eventsByStatus
            };

            return View(viewModel);
        }

        private async Task<int?> ResolveCurrentOrganizationIdAsync()
        {
            var currentUserId = _AppAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return null;
            }

            var organization = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == currentUserId.Value);
            return organization?.Id;
        }
    }
}


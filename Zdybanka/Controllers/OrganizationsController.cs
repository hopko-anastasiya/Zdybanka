using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zdybanka.Models;
using Zdybanka.Models.ViewModels;
using Zdybanka.Services;

namespace Zdybanka.Controllers
{
    public class OrganizationsController : Controller
    {
        private readonly Lab1Context _context;

        public OrganizationsController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Organizations
        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.Organizations.Include(o => o.Status);
            return View(await lab1Context.ToListAsync());
        }

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
        public async Task<IActionResult> Create([Bind("Id,Statusid,Name,Email,Description,Createdat,Updatedat")] Organization organization)
        {
            if (ModelState.IsValid)
            {
                _context.Add(organization);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Statusid"] = new SelectList(_context.Organizationstatuses, "Id", "Statusname", organization.Statusid);
            return View(organization);
        }

        // GET: Organizations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Statusid,Name,Email,Description")] Organization organization, string? returnUrl = null)
        {
            if (id != organization.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    organization.Createdat = _context.Organizations.AsNoTracking().FirstOrDefault(o => o.Id == id)?.Createdat;
                    organization.Updatedat = DateTime.Now;
                    _context.Update(organization);
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
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return View(organization);
        }

        // GET: Organizations/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: Organizations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
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

        public async Task<IActionResult> Profile(int? id = null)
        {
            var organizationId = id ?? TemporaryIdentity.CurrentOrganizationId;

            var organization = await _context.Organizations.Include(o => o.Status).FirstOrDefaultAsync(m => m.Id == organizationId);

            if (organization == null)
            {
                return NotFound();
            }

            return View(organization);
        }

        public async Task<IActionResult> Statistics(int? id = null)
        {
            var organizationId = id ?? TemporaryIdentity.CurrentOrganizationId;

            await EventStatusAutomationService.SynchronizeStatusesAsync(_context, organizationId);

            var organization = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                return NotFound();
            }

            var eventsQuery = _context.Events
                .AsNoTracking()
                .Where(e => e.Organizationid == organizationId);

            var totalEventsCount = await eventsQuery.CountAsync();

            var completedEventsCount = await eventsQuery
                .Where(e => e.Status != null && e.Status.Statusname == "Проведена")
                .CountAsync();

            var totalRegistrationsCount = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.Event != null && r.Event.Organizationid == organizationId)
                .CountAsync();

            var totalFavoritesCount = await _context.Userfavorites
                .AsNoTracking()
                .Where(f => f.Event != null && f.Event.Organizationid == organizationId)
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
                .ThenBy(e => e.Title)
                .Take(3)
                .ToListAsync();

            var viewModel = new OrganizationStatisticsViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                TotalEventsCount = totalEventsCount,
                CompletedEventsCount = completedEventsCount,
                TotalRegistrationsCount = totalRegistrationsCount,
                TotalFavoritesCount = totalFavoritesCount,
                TopEventsByRegistrations = topEvents
            };

            return View(viewModel);
        }
    }
}

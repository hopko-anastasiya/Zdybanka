using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zdybanka.Models;

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Statusid,Name,Email,Description")] Organization organization)
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["Statusid"] = new SelectList(_context.Organizationstatuses, "Id", "Statusname", organization.Statusid);
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
    }
}

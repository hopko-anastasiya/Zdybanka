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
    public class OrganizationstatusController : Controller
    {
        private readonly Lab1Context _context;

        public OrganizationstatusController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Organizationstatus
        public async Task<IActionResult> Index()
        {
            return View(await _context.Organizationstatuses.ToListAsync());
        }

        // GET: Organizationstatus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizationstatus = await _context.Organizationstatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (organizationstatus == null)
            {
                return NotFound();
            }

            return View(organizationstatus);
        }

        // GET: Organizationstatus/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Organizationstatus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Statusname")] Organizationstatus organizationstatus)
        {
            if (ModelState.IsValid)
            {
                _context.Add(organizationstatus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(organizationstatus);
        }

        // GET: Organizationstatus/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizationstatus = await _context.Organizationstatuses.FindAsync(id);
            if (organizationstatus == null)
            {
                return NotFound();
            }
            return View(organizationstatus);
        }

        // POST: Organizationstatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Statusname")] Organizationstatus organizationstatus)
        {
            if (id != organizationstatus.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(organizationstatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrganizationstatusExists(organizationstatus.Id))
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
            return View(organizationstatus);
        }

        // GET: Organizationstatus/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizationstatus = await _context.Organizationstatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (organizationstatus == null)
            {
                return NotFound();
            }

            return View(organizationstatus);
        }

        // POST: Organizationstatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var organizationstatus = await _context.Organizationstatuses.FindAsync(id);
            if (organizationstatus != null)
            {
                _context.Organizationstatuses.Remove(organizationstatus);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrganizationstatusExists(int id)
        {
            return _context.Organizationstatuses.Any(e => e.Id == id);
        }
    }
}

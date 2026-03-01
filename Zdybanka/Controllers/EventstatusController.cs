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
    public class EventstatusController : Controller
    {
        private readonly Lab1Context _context;

        public EventstatusController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Eventstatus
        public async Task<IActionResult> Index()
        {
            return View(await _context.Eventstatuses.ToListAsync());
        }

        // GET: Eventstatus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventstatus = await _context.Eventstatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventstatus == null)
            {
                return NotFound();
            }

            return View(eventstatus);
        }

        // GET: Eventstatus/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Eventstatus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Statusname")] Eventstatus eventstatus)
        {
            if (ModelState.IsValid)
            {
                _context.Add(eventstatus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(eventstatus);
        }

        // GET: Eventstatus/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventstatus = await _context.Eventstatuses.FindAsync(id);
            if (eventstatus == null)
            {
                return NotFound();
            }
            return View(eventstatus);
        }

        // POST: Eventstatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Statusname")] Eventstatus eventstatus)
        {
            if (id != eventstatus.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventstatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventstatusExists(eventstatus.Id))
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
            return View(eventstatus);
        }

        // GET: Eventstatus/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventstatus = await _context.Eventstatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventstatus == null)
            {
                return NotFound();
            }

            return View(eventstatus);
        }

        // POST: Eventstatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventstatus = await _context.Eventstatuses.FindAsync(id);
            if (eventstatus != null)
            {
                _context.Eventstatuses.Remove(eventstatus);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventstatusExists(int id)
        {
            return _context.Eventstatuses.Any(e => e.Id == id);
        }
    }
}

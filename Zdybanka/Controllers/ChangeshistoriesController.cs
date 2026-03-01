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
    public class ChangeshistoriesController : Controller
    {
        private readonly Lab1Context _context;

        public ChangeshistoriesController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Changeshistories
        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.Changeshistories.Include(c => c.Event);
            return View(await lab1Context.ToListAsync());
        }

        // GET: Changeshistories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var changeshistory = await _context.Changeshistories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (changeshistory == null)
            {
                return NotFound();
            }

            return View(changeshistory);
        }

        // GET: Changeshistories/Create
        public IActionResult Create()
        {
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Id");
            return View();
        }

        // POST: Changeshistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Eventid,Changedata,Changedat")] Changeshistory changeshistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(changeshistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Id", changeshistory.Eventid);
            return View(changeshistory);
        }

        // GET: Changeshistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var changeshistory = await _context.Changeshistories.FindAsync(id);
            if (changeshistory == null)
            {
                return NotFound();
            }
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Id", changeshistory.Eventid);
            return View(changeshistory);
        }

        // POST: Changeshistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Eventid,Changedata")] Changeshistory changeshistory)
        {
            if (id != changeshistory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    changeshistory.Changedat = _context.Changeshistories.AsNoTracking().FirstOrDefault(o => o.Id == id)?.Changedat;
                    _context.Update(changeshistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChangeshistoryExists(changeshistory.Id))
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
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Id", changeshistory.Eventid);
            return View(changeshistory);
        }

        // GET: Changeshistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var changeshistory = await _context.Changeshistories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (changeshistory == null)
            {
                return NotFound();
            }

            return View(changeshistory);
        }

        // POST: Changeshistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var changeshistory = await _context.Changeshistories.FindAsync(id);
            if (changeshistory != null)
            {
                _context.Changeshistories.Remove(changeshistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChangeshistoryExists(int id)
        {
            return _context.Changeshistories.Any(e => e.Id == id);
        }
    }
}

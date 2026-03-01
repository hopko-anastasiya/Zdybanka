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
    public class EventcategoriesController : Controller
    {
        private readonly Lab1Context _context;

        public EventcategoriesController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Eventcategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.Eventcategories.ToListAsync());
        }

        // GET: Eventcategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventcategory = await _context.Eventcategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventcategory == null)
            {
                return NotFound();
            }

            return View(eventcategory);
        }

        // GET: Eventcategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Eventcategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Categoryname")] Eventcategory eventcategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(eventcategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(eventcategory);
        }

        // GET: Eventcategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventcategory = await _context.Eventcategories.FindAsync(id);
            if (eventcategory == null)
            {
                return NotFound();
            }
            return View(eventcategory);
        }

        // POST: Eventcategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Categoryname")] Eventcategory eventcategory)
        {
            if (id != eventcategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventcategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventcategoryExists(eventcategory.Id))
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
            return View(eventcategory);
        }

        // GET: Eventcategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventcategory = await _context.Eventcategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventcategory == null)
            {
                return NotFound();
            }

            return View(eventcategory);
        }

        // POST: Eventcategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventcategory = await _context.Eventcategories.FindAsync(id);
            if (eventcategory != null)
            {
                _context.Eventcategories.Remove(eventcategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventcategoryExists(int id)
        {
            return _context.Eventcategories.Any(e => e.Id == id);
        }
    }
}

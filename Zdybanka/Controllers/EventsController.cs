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
    public class EventsController : Controller
    {
        private readonly Lab1Context _context;

        public EventsController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.Events.Include(e => e.Category).Include(e => e.Organization).Include(e => e.Status);
            return View(await lab1Context.ToListAsync());
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
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

            return View(_event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Id");
            ViewData["Organizationid"] = new SelectList(_context.Organizations, "Id", "Id");
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Id");
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Organizationid,Categoryid,Statusid,Title,Location,Description,Eventdate,Createdat,Updatedat")] Event _event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(_event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Id", _event.Categoryid);
            ViewData["Organizationid"] = new SelectList(_context.Organizations, "Id", "Id", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Id", _event.Statusid);
            return View(_event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var _event = await _context.Events.FindAsync(id);
            if (_event == null)
            {
                return NotFound();
            }
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Id", _event.Categoryid);
            ViewData["Organizationid"] = new SelectList(_context.Organizations, "Id", "Id", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Id", _event.Statusid);
            return View(_event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Organizationid,Categoryid,Statusid,Title,Location,Description,Eventdate")] Event _event)
        {
            if (id != _event.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _event.Createdat = _context.Events.AsNoTracking().FirstOrDefault(o => o.Id == id)?.Createdat;
                    _event.Updatedat = DateTime.Now;
                    _context.Update(_event);
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
            ViewData["Categoryid"] = new SelectList(_context.Eventcategories, "Id", "Id", _event.Categoryid);
            ViewData["Organizationid"] = new SelectList(_context.Organizations, "Id", "Id", _event.Organizationid);
            ViewData["Statusid"] = new SelectList(_context.Eventstatuses, "Id", "Id", _event.Statusid);
            return View(_event);
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
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

            return View(_event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var _event = await _context.Events.FindAsync(id);
            if (_event != null)
            {
                _context.Events.Remove(_event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}

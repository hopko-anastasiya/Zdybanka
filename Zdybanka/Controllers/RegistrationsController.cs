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
    public class RegistrationsController : Controller
    {
        private readonly Lab1Context _context;

        public RegistrationsController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Registrations
        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.Registrations.Include(r => r.Event).Include(r => r.User);
            return View(await lab1Context.ToListAsync());
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

            return View(registration);
        }

        // GET: Registrations/Create
        public IActionResult Create()
        {
            var allowedEvents = _context.Events.Include(e => e.Status)
                .Where(e => e.Status == null || (e.Status.Statusname != "Проведена" && e.Status.Statusname != "Відмінена"));
            ViewData["Eventid"] = new SelectList(allowedEvents, "Id", "Title");
            ViewData["Userid"] = new SelectList(_context.Users, "Id", "Fullname");
            return View();
        }

        // POST: Registrations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Userid,Eventid,Registrationdate")] Registration registration)
        {
            var selectedEvent = await _context.Events.Include(e => e.Status).FirstOrDefaultAsync(e => e.Id == registration.Eventid);
            if (selectedEvent?.Status != null && (selectedEvent.Status.Statusname == "Проведена" || selectedEvent.Status.Statusname == "Відмінена"))
            {
                ModelState.AddModelError("Eventid", "Неможливо зареєструватися на подію зі статусом '" + selectedEvent.Status.Statusname + "'.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(registration);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var allowedEvents = _context.Events.Include(e => e.Status)
                .Where(e => e.Status == null || (e.Status.Statusname != "Проведена" && e.Status.Statusname != "Відмінена"));
            ViewData["Eventid"] = new SelectList(allowedEvents, "Id", "Title", registration.Eventid);
            ViewData["Userid"] = new SelectList(_context.Users, "Id", "Fullname", registration.Userid);
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
                _context.Registrations.Remove(registration);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RegistrationExists(int id)
        {
            return _context.Registrations.Any(e => e.Id == id);
        }
    }
}

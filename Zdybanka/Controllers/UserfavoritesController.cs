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
    public class UserfavoritesController : Controller
    {
        private readonly Lab1Context _context;

        public UserfavoritesController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Userfavorites
        public async Task<IActionResult> Index()
        {
            var currentUserId = TemporaryIdentity.CurrentUserId;
            var lab1Context = _context.Userfavorites
                .Where(u => u.Userid == currentUserId)
                .Include(u => u.Event)
                .Include(u => u.User);
            return View(await lab1Context.ToListAsync());
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

            return View(userfavorite);
        }

        // GET: Userfavorites/Create
        public IActionResult Create()
        {
            var currentUserId = TemporaryIdentity.CurrentUserId;
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Title");
            ViewData["Userid"] = new SelectList(_context.Users.Where(u => u.Id == currentUserId), "Id", "Fullname", currentUserId);
            return View();
        }

        // POST: Userfavorites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Userid,Eventid")] Userfavorite userfavorite)
        {
            var currentUserId = TemporaryIdentity.CurrentUserId;
            userfavorite.Userid = currentUserId;

            if (ModelState.IsValid)
            {
                _context.Add(userfavorite);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Eventid"] = new SelectList(_context.Events, "Id", "Title", userfavorite.Eventid);
            ViewData["Userid"] = new SelectList(_context.Users.Where(u => u.Id == currentUserId), "Id", "Fullname", currentUserId);
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
                _context.Userfavorites.Remove(userfavorite);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserfavoriteExists(int id)
        {
            return _context.Userfavorites.Any(e => e.Id == id);
        }
    }
}

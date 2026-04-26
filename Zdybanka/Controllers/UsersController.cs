using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Zdybanka.Models;

namespace Zdybanka.Controllers
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class UsersController : Controller
    {
        private readonly Lab1Context _context;

        public UsersController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? role = null)
        {
            IQueryable<User> usersQuery = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
                {
                    usersQuery = usersQuery.Where(u => u.Role == parsedRole);
                }
                else
                {
                    ModelState.AddModelError("role", "Невідома роль для фільтрації.");
                }
            }

            ViewData["SelectedRole"] = role;
            ViewData["RoleOptions"] = BuildRoleSelectList(role);

            return View(await usersQuery
                .OrderBy(u => u.Fullname)
                .ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["RoleOptions"] = BuildRoleSelectList(UserRole.User.ToString());
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Fullname,Email,PasswordHash,Role,Createdat")] User user)
        {
            if (ModelState.IsValid)
            {
                if (!user.Createdat.HasValue)
                {
                    user.Createdat = DateTime.Now;
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["RoleOptions"] = BuildRoleSelectList(user.Role.ToString());
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["RoleOptions"] = BuildRoleSelectList(user.Role.ToString());
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Fullname,Email,PasswordHash,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(o => o.Id == id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    existingUser.Fullname = user.Fullname;
                    existingUser.Email = user.Email;
                    existingUser.PasswordHash = user.PasswordHash;
                    existingUser.Role = user.Role;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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

            ViewData["RoleOptions"] = BuildRoleSelectList(user.Role.ToString());
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        private static SelectList BuildRoleSelectList(string? selectedRole)
        {
            var items = new List<SelectListItem>
            {
                new() { Value = UserRole.Admin.ToString(), Text = "admin" },
                new() { Value = UserRole.OrganizationManager.ToString(), Text = "organization_manager" },
                new() { Value = UserRole.User.ToString(), Text = "user" }
            };

            return new SelectList(items, "Value", "Text", selectedRole);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        private static readonly Dictionary<string, string> FieldNames = new()
        {
            {"Title", "Назва"},
            {"Location", "Місце проведення"},
            {"Description", "Опис"},
            {"Eventdate", "Дата та час події"},
            {"Organizationid", "Організація"},
            {"Categoryid", "Категорія"},
            {"Statusid", "Статус"},
            {"Createdat", "Дата створення"},
            {"Updatedat", "Дата оновлення"}
        };

        public ChangeshistoriesController(Lab1Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Парсить JSON з Changedata і повертає список читабельних змін.
        /// </summary>
        public static List<ChangeEntry> ParseChanges(string? json)
        {
            var result = new List<ChangeEntry>();
            if (string.IsNullOrEmpty(json)) return result;

            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name.StartsWith("_")) continue;

                    var label = FieldNames.TryGetValue(prop.Name, out var name) ? name : prop.Name;
                    var oldVal = prop.Value.TryGetProperty("Old", out var o) ? o.ToString() : "";
                    var newVal = prop.Value.TryGetProperty("New", out var n) ? n.ToString() : "";
                    result.Add(new ChangeEntry { Field = label, OldValue = oldVal, NewValue = newVal });
                }
            }
            catch
            {
                result.Add(new ChangeEntry { Field = "Дані", OldValue = "", NewValue = json });
            }

            return result;
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
    }

    public class ChangeEntry
    {
        public string Field { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }
}

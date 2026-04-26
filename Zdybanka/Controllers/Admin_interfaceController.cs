using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zdybanka.Models;
using Zdybanka.Models.ViewModels;

namespace Zdybanka.Controllers
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class Admin_interfaceController : Controller
    {
        private readonly Lab1Context _context;

        public Admin_interfaceController(Lab1Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Organizations));
        }

        public async Task<IActionResult> Organizations(string? search)
        {
            var organizations = await LoadOrganizationsPageAsync(search);
            return View(organizations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> ImportOrganizations(IFormFile? csvFile, string? search)
        {
            var importResult = await ImportOrganizationsFromCsvAsync(csvFile);
            var organizations = await LoadOrganizationsPageAsync(search);

            ViewData["OrganizationImportResult"] = importResult;

            return View("Organizations", organizations);
        }

        public async Task<IActionResult> Category()
        {
            var categories = await _context.Eventcategories
                .AsNoTracking()
                .OrderBy(c => c.Categoryname)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Events()
        {
            var events = await _context.Events
                .AsNoTracking()
                .Include(e => e.Organization)
                .Include(e => e.Category)
                .Include(e => e.Status)
                .OrderBy(e =>
                    e.Status != null && (e.Status.Statusname == "Запланована" || e.Status.Statusname == "Заплановано") ? 0 :
                    e.Status != null && e.Status.Statusname == "Відмінена" ? 1 :
                    e.Status != null && e.Status.Statusname == "Проведена" ? 2 : 3)
                .ThenByDescending(e => e.Createdat)
                .ToListAsync();

            var eventIdsWithHistory = await _context.Changeshistories
                .AsNoTracking()
                .Where(h => h.Eventid.HasValue)
                .Select(h => h.Eventid!.Value)
                .Distinct()
                .ToListAsync();

            ViewData["EventIdsWithHistory"] = new HashSet<int>(eventIdsWithHistory);

            return View(events);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEvent(int id)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity == null)
            {
                TempData["AdminEventsError"] = "Подію не знайдено.";
                return RedirectToAction(nameof(Events));
            }

            var canceledStatus = await _context.Eventstatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Statusname == "Відмінена");

            if (canceledStatus == null)
            {
                TempData["AdminEventsError"] = "Не знайдено статус 'Відмінена'.";
                return RedirectToAction(nameof(Events));
            }

            if (eventEntity.Statusid == canceledStatus.Id)
            {
                TempData["AdminEventsError"] = "Подія вже має статус 'Відмінена'.";
                return RedirectToAction(nameof(Events));
            }

            eventEntity.Statusid = canceledStatus.Id;
            eventEntity.Updatedat = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["AdminEventsSuccess"] = $"Подію \"{eventEntity.Title}\" успішно відмінено.";
            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> EventHistory(int id)
        {
            var historyRows = await _context.Changeshistories
                .AsNoTracking()
                .Where(h => h.Eventid == id)
                .OrderByDescending(h => h.Changedat)
                .ToListAsync();

            var result = new List<object>();

            foreach (var row in historyRows)
            {
                var changes = await ParseHistoryChangesAsync(row.Changedata);
                if (changes.Count == 0)
                {
                    continue;
                }

                result.Add(new
                {
                    changedAt = row.Changedat?.ToString("dd.MM.yyyy HH:mm") ?? "-",
                    changes
                });
            }

            return Json(result);
        }

        private static readonly Dictionary<string, string> EventFieldLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Title"] = "Назва",
            ["Location"] = "Місце",
            ["Description"] = "Опис",
            ["Eventdate"] = "Дата події",
            ["Organizationid"] = "Організація",
            ["Categoryid"] = "Категорія",
            ["Statusid"] = "Статус",
            ["Createdat"] = "Дата створення",
            ["Updatedat"] = "Дата оновлення"
        };

        private async Task<List<object>> ParseHistoryChangesAsync(string? changedata)
        {
            var parsedChanges = new List<object>();
            if (string.IsNullOrWhiteSpace(changedata))
            {
                return parsedChanges;
            }

            try
            {
                using var doc = JsonDocument.Parse(changedata);
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if (property.Name.StartsWith("_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var oldRaw = TryGetJsonValue(property.Value, "Old");
                    var newRaw = TryGetJsonValue(property.Value, "New");

                    var oldDisplay = await ResolveFieldDisplayValueAsync(property.Name, oldRaw);
                    var newDisplay = await ResolveFieldDisplayValueAsync(property.Name, newRaw);

                    parsedChanges.Add(new
                    {
                        field = EventFieldLabels.TryGetValue(property.Name, out var label) ? label : property.Name,
                        oldValue = oldDisplay,
                        newValue = newDisplay
                    });
                }
            }
            catch
            {
                return new List<object>();
            }

            return parsedChanges;
        }

        private static string? TryGetJsonValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => property.GetString(),
                _ => property.ToString()
            };
        }

        private async Task<string> ResolveFieldDisplayValueAsync(string fieldName, string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return "-";
            }

            if (fieldName.Equals("Categoryid", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(rawValue, out var categoryId))
            {
                var category = await _context.Eventcategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId);
                return category?.Categoryname ?? rawValue;
            }

            if (fieldName.Equals("Statusid", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(rawValue, out var statusId))
            {
                var status = await _context.Eventstatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == statusId);
                return status?.Statusname ?? rawValue;
            }

            if (fieldName.Equals("Organizationid", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(rawValue, out var organizationId))
            {
                var organization = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId);
                return organization?.Fullname ?? rawValue;
            }

            if (fieldName.Equals("Eventdate", StringComparison.OrdinalIgnoreCase)
                || fieldName.Equals("Createdat", StringComparison.OrdinalIgnoreCase)
                || fieldName.Equals("Updatedat", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParse(rawValue, out var dateValue))
                {
                    return dateValue.ToString("dd.MM.yyyy HH:mm");
                }
            }

            return rawValue;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleOrganizationBlock(int id)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                TempData["AdminOrganizationsError"] = "Організацію не знайдено.";
                return RedirectToAction(nameof(Organizations));
            }

            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var blockedStatus = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Заблокована", StringComparison.OrdinalIgnoreCase));

            var verifiedStatus = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Верифікована", StringComparison.OrdinalIgnoreCase));

            if (blockedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Заблокована'.";
                return RedirectToAction(nameof(Organizations));
            }

            if (verifiedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Верифікована'.";
                return RedirectToAction(nameof(Organizations));
            }

            if (blockedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Заблокована'.";
                return RedirectToAction(nameof(Organizations));
            }

            if (verifiedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Верифікована'.";
                return RedirectToAction(nameof(Organizations));
            }

            var isBlocked = organization.Statusid == blockedStatus.Id;

            organization.Statusid = isBlocked ? verifiedStatus.Id : blockedStatus.Id;

            await _context.SaveChangesAsync();

            TempData["AdminOrganizationsSuccess"] = isBlocked
                ? $"Організацію \"{organization.Fullname}\" успішно розблоковано."
                : $"Організацію \"{organization.Fullname}\" успішно заблоковано.";

            return RedirectToAction(nameof(Organizations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrganizationVerification(int id)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                TempData["AdminOrganizationsError"] = "Організацію не знайдено.";
                return RedirectToAction(nameof(Organizations));
            }

            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var verifiedStatus = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Верифікована", StringComparison.OrdinalIgnoreCase));

            if (verifiedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Верифікована'.";
                return RedirectToAction(nameof(Organizations));
            }

            organization.Statusid = verifiedStatus.Id;

            await _context.SaveChangesAsync();

            TempData["AdminOrganizationsSuccess"] = $"Верифікацію організації \"{organization.Fullname}\" підтверджено.";
            return RedirectToAction(nameof(Organizations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrganizationVerification(int id)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                TempData["AdminOrganizationsError"] = "Організацію не знайдено.";
                return RedirectToAction(nameof(Organizations));
            }

            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var unverifiedStatus = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Не верифікована", StringComparison.OrdinalIgnoreCase));

            if (unverifiedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Не верифікована'.";
                return RedirectToAction(nameof(Organizations));
            }

            organization.Statusid = unverifiedStatus.Id;

            await _context.SaveChangesAsync();

            TempData["AdminOrganizationsSuccess"] = $"Заявку організації \"{organization.Fullname}\" відхилено.";
            return RedirectToAction(nameof(Organizations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrganizationUnblock(int id)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                TempData["AdminOrganizationsError"] = "Організацію не знайдено.";
                return RedirectToAction(nameof(Organizations));
            }

            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var verifiedStatus = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Верифікована", StringComparison.OrdinalIgnoreCase));

            if (verifiedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Верифікована'.";
                return RedirectToAction(nameof(Organizations));
            }

            organization.Statusid = verifiedStatus.Id;

            await _context.SaveChangesAsync();

            TempData["AdminOrganizationsSuccess"] = $"Заявку на розблокування організації \"{organization.Fullname}\" підтверджено.";
            return RedirectToAction(nameof(Organizations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrganizationUnblock(int id)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                TempData["AdminOrganizationsError"] = "Організацію не знайдено.";
                return RedirectToAction(nameof(Organizations));
            }

            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var blockedStatus = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Заблокована", StringComparison.OrdinalIgnoreCase));

            if (blockedStatus == null)
            {
                TempData["AdminOrganizationsError"] = "Не знайдено статус 'Заблокована'.";
                return RedirectToAction(nameof(Organizations));
            }

            organization.Statusid = blockedStatus.Id;

            await _context.SaveChangesAsync();

            TempData["AdminOrganizationsSuccess"] = $"Заявку на розблокування організції \"{organization.Fullname}\" відхилено.";
            return RedirectToAction(nameof(Organizations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["AdminCategoryError"] = "Назва категорії не може бути порожною.";
                return RedirectToAction(nameof(Category));
            }

            var normalizedName = categoryName.Trim();
            
            var existingCategory = await _context.Eventcategories
                .FirstOrDefaultAsync(c => c.Categoryname.ToLower() == normalizedName.ToLower());

            if (existingCategory != null)
            {
                TempData["AdminCategoryError"] = "Категорія з такою назвою вже існує.";
                return RedirectToAction(nameof(Category));
            }

            var newCategory = new Eventcategory
            {
                Categoryname = normalizedName
            };

            _context.Eventcategories.Add(newCategory);
            await _context.SaveChangesAsync();

            TempData["AdminCategorySuccess"] = $"Категорію \"{normalizedName}\" успішно створено.";
            return RedirectToAction(nameof(Category));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Eventcategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                TempData["AdminCategoryError"] = "Категорію не знайдено.";
                return RedirectToAction(nameof(Category));
            }

            var eventsWithCategory = await _context.Events
                .Where(e => e.Categoryid == id)
                .ToListAsync();

            if (eventsWithCategory.Any())
            {
                TempData["AdminCategoryError"] = $"Неможливо видалити категорію \"{category.Categoryname}\", оскільки вона використовується {eventsWithCategory.Count} подією(ями).";
                return RedirectToAction(nameof(Category));
            }

            _context.Eventcategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["AdminCategorySuccess"] = $"Категорію \"{category.Categoryname}\" успішно видалено.";
            return RedirectToAction(nameof(Category));
        }

        private async Task<List<Organization>> LoadOrganizationsPageAsync(string? search)
        {
            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var pendingStatusId = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Очікує підтвердження", StringComparison.OrdinalIgnoreCase))
                ?.Id;

            var unverifiedStatusId = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Не верифікована", StringComparison.OrdinalIgnoreCase))
                ?.Id;

            var pendingUnblockStatusId = statuses
                .FirstOrDefault(s => string.Equals(s.Statusname, "Очікує розблокування", StringComparison.OrdinalIgnoreCase))
                ?.Id;

            var hiddenStatusIds = new List<int>();
            if (unverifiedStatusId.HasValue)
            {
                hiddenStatusIds.Add(unverifiedStatusId.Value);
            }

            if (pendingStatusId.HasValue)
            {
                hiddenStatusIds.Add(pendingStatusId.Value);
            }

            if (pendingUnblockStatusId.HasValue)
            {
                hiddenStatusIds.Add(pendingUnblockStatusId.Value);
            }

            var pendingOrganizations = pendingStatusId.HasValue
                ? await _context.Organizations
                    .AsNoTracking()
                    .Include(o => o.Status)
                    .Where(o => o.Statusid == pendingStatusId.Value)
                    .OrderBy(o => o.Fullname)
                    .ToListAsync()
                : new List<Organization>();

            var unblockRequestOrganizations = pendingUnblockStatusId.HasValue
                ? await _context.Organizations
                    .AsNoTracking()
                    .Include(o => o.Status)
                    .Where(o => o.Statusid == pendingUnblockStatusId.Value)
                    .OrderBy(o => o.Fullname)
                    .ToListAsync()
                : new List<Organization>();

            var organizationsQuery = _context.Organizations
                .AsNoTracking()
                .Where(o => !o.Statusid.HasValue || !hiddenStatusIds.Contains(o.Statusid.Value))
                .Include(o => o.Status)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();
                organizationsQuery = organizationsQuery
                    .Where(o => o.Fullname.ToLower().Contains(normalizedSearch));
            }

            var organizations = await organizationsQuery
                .OrderBy(o => o.Fullname)
                .ToListAsync();

            ViewData["OrganizationSearch"] = search;
            ViewData["PendingOrganizations"] = pendingOrganizations;
            ViewData["UnblockRequestOrganizations"] = unblockRequestOrganizations;

            return organizations;
        }

        private async Task<OrganizationImportResultViewModel> ImportOrganizationsFromCsvAsync(IFormFile? csvFile)
        {
            var result = new OrganizationImportResultViewModel();

            if (csvFile == null || csvFile.Length == 0)
            {
                result.Messages.Add(new OrganizationImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "Файл для імпорту не вибрано або він порожній."
                });

                return result;
            }

            var fileExtension = Path.GetExtension(csvFile.FileName);
            if (!string.Equals(fileExtension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.Messages.Add(new OrganizationImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "Потрібно завантажити саме CSV-файл з розширенням .csv."
                });

                return result;
            }

            string fileContent;
            await using (var stream = csvFile.OpenReadStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                fileContent = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                result.Messages.Add(new OrganizationImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "CSV-файл не містить даних."
                });

                return result;
            }

            var delimiter = DetectCsvDelimiter(fileContent);
            var records = ParseCsvRecords(fileContent, delimiter);

            if (records.Count == 0)
            {
                result.Messages.Add(new OrganizationImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "CSV-файл не містить жодного рядка для обробки."
                });

                return result;
            }

            var statuses = await _context.Organizationstatuses
                .AsNoTracking()
                .ToListAsync();

            var defaultStatus = statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Не верифікована", StringComparison.OrdinalIgnoreCase))
                ?? statuses.FirstOrDefault(s => string.Equals(s.Statusname, "Очікує підтвердження", StringComparison.OrdinalIgnoreCase));

            var statusLookup = statuses
                .Where(s => !string.IsNullOrWhiteSpace(s.Statusname))
                .ToDictionary(s => NormalizeLookupValue(s.Statusname), s => s);

            var existingOrganizations = await _context.Organizations
                .AsNoTracking()
                .Select(o => new { o.Fullname, o.Email })
                .ToListAsync();

            var existingNames = new HashSet<string>(existingOrganizations
                .Where(o => !string.IsNullOrWhiteSpace(o.Fullname))
                .Select(o => NormalizeLookupValue(o.Fullname)));

            var existingEmails = new HashSet<string>(existingOrganizations
                .Where(o => !string.IsNullOrWhiteSpace(o.Email))
                .Select(o => NormalizeLookupValue(o.Email)));

            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var hasHeaderRow = TryBuildHeaderMap(records[0], out var headerMap);
            var startIndex = hasHeaderRow ? 1 : 0;

            if (hasHeaderRow && !HasRequiredHeaderColumns(headerMap))
            {
                result.Messages.Add(new OrganizationImportMessageViewModel
                {
                    RowNumber = 1,
                    IsSuccess = false,
                    Message = "CSV-заголовок має містити принаймні колонки Name/Назва та Email/Пошта."
                });

                return result;
            }

            for (var recordIndex = startIndex; recordIndex < records.Count; recordIndex++)
            {
                var rowNumber = recordIndex + 1;
                var row = records[recordIndex];

                if (IsEmptyRow(row))
                {
                    result.Messages.Add(new OrganizationImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = "Рядок порожній і був пропущений."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                var name = GetCsvValue(row, headerMap, hasHeaderRow, 0, "name", "назва", "organization", "організація")?.Trim();
                var email = GetCsvValue(row, headerMap, hasHeaderRow, 1, "email", "пошта", "mail")?.Trim();
                var description = GetCsvValue(row, headerMap, hasHeaderRow, 2, "description", "опис")?.Trim();
                var statusName = GetCsvValue(row, headerMap, hasHeaderRow, 3, "status", "статус")?.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Messages.Add(new OrganizationImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = "Не вказано назву організації."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    result.Messages.Add(new OrganizationImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"Організацію \"{name}\" не додано: не вказано email."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (!IsValidEmail(email))
                {
                    result.Messages.Add(new OrganizationImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"Організацію \"{name}\" не додано: email \"{email}\" має некоректний формат."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                var normalizedName = NormalizeLookupValue(name);
                var normalizedEmail = NormalizeLookupValue(email);

                if (existingNames.Contains(normalizedName) || seenNames.Contains(normalizedName))
                {
                    result.Messages.Add(new OrganizationImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"Організацію \"{name}\" не додано: організація з такою назвою вже існує."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                if (existingEmails.Contains(normalizedEmail) || seenEmails.Contains(normalizedEmail))
                {
                    result.Messages.Add(new OrganizationImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"Організацію \"{name}\" не додано: організація з таким email вже існує."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                Organizationstatus? selectedStatus = defaultStatus;
                if (!string.IsNullOrWhiteSpace(statusName))
                {
                    var normalizedStatus = NormalizeLookupValue(statusName);
                    if (!statusLookup.TryGetValue(normalizedStatus, out selectedStatus))
                    {
                        result.Messages.Add(new OrganizationImportMessageViewModel
                        {
                            RowNumber = rowNumber,
                            IsSuccess = false,
                            Message = $"Організацію \"{name}\" не додано: статус \"{statusName}\" не знайдено."
                        });

                        result.SkippedCount += 1;
                        continue;
                    }
                }

                var organization = new Organization
                {
                    Fullname = name,
                    Email = email,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    Statusid = selectedStatus?.Id,
                    Createdat = DateTime.Now,
                    PasswordHash = Guid.NewGuid().ToString("N"),
                    Role = UserRole.OrganizationManager
                };

                _context.Organizations.Add(organization);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    _context.Entry(organization).State = EntityState.Detached;
                    var dbError = ex.InnerException?.Message ?? ex.Message;
                    result.Messages.Add(new OrganizationImportMessageViewModel
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        Message = $"Організацію \"{name}\" не додано: помилка БД ({dbError})."
                    });

                    result.SkippedCount += 1;
                    continue;
                }

                existingNames.Add(normalizedName);
                existingEmails.Add(normalizedEmail);
                seenNames.Add(normalizedName);
                seenEmails.Add(normalizedEmail);

                result.ImportedCount += 1;
            }

            if (result.Messages.Count == 0)
            {
                result.Messages.Add(new OrganizationImportMessageViewModel
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    Message = "CSV-файл не містить даних для імпорту."
                });
            }

            return result;
        }

        private static bool IsValidEmail(string email)
        {
            return new EmailAddressAttribute().IsValid(email);
        }

        private static bool IsEmptyRow(IReadOnlyCollection<string> row)
        {
            return row.All(string.IsNullOrWhiteSpace);
        }

        private static bool TryBuildHeaderMap(IReadOnlyList<string> firstRow, out Dictionary<string, int> headerMap)
        {
            headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var hasKnownHeader = false;

            for (var index = 0; index < firstRow.Count; index++)
            {
                var normalizedHeader = NormalizeLookupValue(firstRow[index]);
                if (string.IsNullOrWhiteSpace(normalizedHeader))
                {
                    continue;
                }

                if (OrganizationHeaderAliases.Contains(normalizedHeader))
                {
                    headerMap[normalizedHeader] = index;
                    hasKnownHeader = true;
                }
            }

            return hasKnownHeader;
        }

        private static bool HasRequiredHeaderColumns(IReadOnlyDictionary<string, int> headerMap)
        {
            return HasHeaderColumn(headerMap, "name", "назва", "organization", "організація")
                && HasHeaderColumn(headerMap, "email", "пошта", "mail");
        }

        private static bool HasHeaderColumn(IReadOnlyDictionary<string, int> headerMap, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                if (headerMap.ContainsKey(NormalizeLookupValue(alias)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string? GetCsvValue(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int>? headerMap,
            bool hasHeaderRow,
            int fallbackIndex,
            params string[] aliases)
        {
            if (hasHeaderRow && headerMap != null)
            {
                foreach (var alias in aliases)
                {
                    var normalizedAlias = NormalizeLookupValue(alias);
                    if (headerMap.TryGetValue(normalizedAlias, out var index) && index < row.Count)
                    {
                        return row[index];
                    }
                }

                return null;
            }

            return fallbackIndex < row.Count ? row[fallbackIndex] : null;
        }

        private static char DetectCsvDelimiter(string content)
        {
            var firstLine = content
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;

            var semicolonCount = firstLine.Count(character => character == ';');
            var commaCount = firstLine.Count(character => character == ',');

            return semicolonCount >= commaCount ? ';' : ',';
        }

        private static List<List<string>> ParseCsvRecords(string content, char delimiter)
        {
            var records = new List<List<string>>();
            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            var isInsideQuotes = false;

            for (var index = 0; index < content.Length; index++)
            {
                var character = content[index];

                if (isInsideQuotes)
                {
                    if (character == '"')
                    {
                        var nextIndex = index + 1;
                        if (nextIndex < content.Length && content[nextIndex] == '"')
                        {
                            currentCell.Append('"');
                            index = nextIndex;
                        }
                        else
                        {
                            isInsideQuotes = false;
                        }
                    }
                    else
                    {
                        currentCell.Append(character);
                    }

                    continue;
                }

                if (character == '"')
                {
                    isInsideQuotes = true;
                    continue;
                }

                if (character == delimiter)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    continue;
                }

                if (character == '\r')
                {
                    continue;
                }

                if (character == '\n')
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    records.Add(currentRow);
                    currentRow = new List<string>();
                    continue;
                }

                currentCell.Append(character);
            }

            currentRow.Add(currentCell.ToString());
            if (currentRow.Count > 1 || !string.IsNullOrWhiteSpace(currentRow[0]) || records.Count == 0)
            {
                records.Add(currentRow);
            }

            return records;
        }

        private static string NormalizeLookupValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value.Trim())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            return builder.ToString();
        }

        private static readonly HashSet<string> OrganizationHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeLookupValue("name"),
            NormalizeLookupValue("назва"),
            NormalizeLookupValue("organization"),
            NormalizeLookupValue("організація"),
            NormalizeLookupValue("orgname"),
            NormalizeLookupValue("email"),
            NormalizeLookupValue("пошта"),
            NormalizeLookupValue("mail"),
            NormalizeLookupValue("description"),
            NormalizeLookupValue("опис"),
            NormalizeLookupValue("status"),
            NormalizeLookupValue("статус")
        };
    }
}

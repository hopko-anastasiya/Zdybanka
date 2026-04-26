using System.Collections.Generic;

namespace Zdybanka.Models.ViewModels;

public class EventImportResultViewModel
{
    public int ImportedCount { get; set; }

    public int SkippedCount { get; set; }

    public List<EventImportMessageViewModel> Messages { get; set; } = new();

    public string Summary => $"Імпорт завершено: додано {ImportedCount}, пропущено {SkippedCount}.";
}

public class EventImportMessageViewModel
{
    public int RowNumber { get; set; }

    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;
}

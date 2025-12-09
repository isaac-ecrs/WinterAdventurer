using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MigraDoc.DocumentObjectModel;
using WinterAdventurer.Library;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;

namespace WinterAdventurer.Test.Mocks;

/// <summary>
/// Mock implementation of ExcelUtilities for testing component behavior
/// without actual Excel parsing or PDF generation.
/// </summary>
public class MockExcelUtilities : ExcelUtilities
{
    public List<Workshop> MockWorkshops { get; set; } = new();
    public bool ThrowOnImport { get; set; }
    public bool ThrowOnCreatePdf { get; set; }
    public Document? DocumentToReturn { get; set; }
    public int ImportExcelCallCount { get; private set; }
    public int CreatePdfCallCount { get; private set; }
    public int CreateMasterScheduleCallCount { get; private set; }

    // Track parameters from last CreatePdf call
    public bool LastMergeWorkshopCells { get; private set; }
    public List<TimeSlot>? LastTimeslots { get; private set; }
    public int LastBlankScheduleCount { get; private set; }
    public string LastEventName { get; private set; } = string.Empty;

    public MockExcelUtilities() : base(NullLogger<ExcelUtilities>.Instance)
    {
    }

    public MockExcelUtilities(ILogger<ExcelUtilities> logger) : base(logger)
    {
    }

    public new void ImportExcel(Stream stream)
    {
        ImportExcelCallCount++;

        if (ThrowOnImport)
        {
            throw new InvalidOperationException("Mock ImportExcel failure");
        }

        // Clear and populate Workshops with mock data
        Workshops.Clear();
        Workshops.AddRange(MockWorkshops);
    }

    public new Document? CreatePdf(bool mergeWorkshopCells, List<TimeSlot>? timeslots, int blankScheduleCount, string eventName)
    {
        CreatePdfCallCount++;
        LastMergeWorkshopCells = mergeWorkshopCells;
        LastTimeslots = timeslots;
        LastBlankScheduleCount = blankScheduleCount;
        LastEventName = eventName;

        if (ThrowOnCreatePdf)
        {
            throw new InvalidOperationException("Mock CreatePdf failure");
        }

        return DocumentToReturn ?? new Document();
    }

    public new Document? CreateMasterSchedulePdf(string eventName, List<TimeSlot>? timeslots)
    {
        CreateMasterScheduleCallCount++;

        if (ThrowOnCreatePdf)
        {
            throw new InvalidOperationException("Mock CreateMasterSchedulePdf failure");
        }

        return DocumentToReturn ?? new Document();
    }

    public void Reset()
    {
        ImportExcelCallCount = 0;
        CreatePdfCallCount = 0;
        CreateMasterScheduleCallCount = 0;
        ThrowOnImport = false;
        ThrowOnCreatePdf = false;
        MockWorkshops.Clear();
        Workshops.Clear();
        DocumentToReturn = null;
        LastMergeWorkshopCells = false;
        LastTimeslots = null;
        LastBlankScheduleCount = 0;
        LastEventName = string.Empty;
    }
}

/// <summary>
/// Mock implementation of ITimeslotValidationService for testing validation scenarios.
/// </summary>
public class MockTimeslotValidationService : ITimeslotValidationService
{
    public ValidationResult ResultToReturn { get; set; } = new ValidationResult();
    public int ValidateCallCount { get; private set; }
    public IEnumerable<TimeSlotDto>? LastTimeslotsValidated { get; private set; }

    public ValidationResult ValidateTimeslots(IEnumerable<TimeSlotDto> timeslots)
    {
        ValidateCallCount++;
        LastTimeslotsValidated = timeslots.ToList();
        return ResultToReturn;
    }

    public void Reset()
    {
        ValidateCallCount = 0;
        LastTimeslotsValidated = null;
        ResultToReturn = new ValidationResult();
    }
}

/// <summary>
/// Mock implementation of TourService for testing tour-related component behavior.
/// Does not require JSInterop, allowing for synchronous testing.
/// </summary>
public class MockTourService
{
    public bool TourCompletedValue { get; set; }
    public List<string> StartedTours { get; } = new();
    public Dictionary<string, int> ResetTours { get; } = new();

    public Task<bool> HasCompletedTourAsync(string tourId)
    {
        return Task.FromResult(TourCompletedValue);
    }

    public Task StartHomeTourAsync()
    {
        StartedTours.Add("home");
        return Task.CompletedTask;
    }

    public Task ResetAndStartTourAsync(string tourId)
    {
        if (!ResetTours.ContainsKey(tourId))
        {
            ResetTours[tourId] = 0;
        }
        ResetTours[tourId]++;

        if (tourId == "home")
        {
            StartedTours.Add("home");
        }

        return Task.CompletedTask;
    }

    public void Reset()
    {
        TourCompletedValue = false;
        StartedTours.Clear();
        ResetTours.Clear();
    }
}

/// <summary>
/// Mock implementation of ThemeService for testing theme-related component behavior.
/// Does not require JSInterop, allowing for synchronous testing.
/// </summary>
public class MockThemeService
{
    private bool _isDarkMode = true;
    public int ToggleCount { get; private set; }
    public int InitializeCount { get; private set; }
    public int ThemeChangedInvocationCount { get; private set; }

    public event Action? OnThemeChanged;

    public bool IsDarkMode => _isDarkMode;

    public Task InitializeAsync()
    {
        InitializeCount++;
        return Task.CompletedTask;
    }

    public Task ToggleThemeAsync()
    {
        ToggleCount++;
        _isDarkMode = !_isDarkMode;
        ThemeChangedInvocationCount++;
        OnThemeChanged?.Invoke();
        return Task.CompletedTask;
    }

    public void SetDarkMode(bool isDarkMode)
    {
        _isDarkMode = isDarkMode;
        ThemeChangedInvocationCount++;
        OnThemeChanged?.Invoke();
    }

    public void Reset()
    {
        _isDarkMode = true;
        ToggleCount = 0;
        InitializeCount = 0;
        ThemeChangedInvocationCount = 0;
    }
}

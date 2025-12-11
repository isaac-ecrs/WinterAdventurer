using System.Text.Json;
using Microsoft.Extensions.Logging;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using WinterAdventurer.Library;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;

// Create logger factory for console logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<ExcelUtilities>();

Console.WriteLine("WinterAdventurer CLI - Class Roster Generator");
Console.WriteLine("=============================================\n");

if (args.Length == 0)
{
    Console.WriteLine("Please provide the path to the Excel file as an argument.");
    Console.WriteLine("Usage: WinterAdventurer.CLI <excel-file> [--timeslots <json-file>] [--no-merge-workshops] [--blank-schedules <count>] [--event-name <name>]");
    Console.WriteLine("\nArguments:");
    Console.WriteLine("  excel-file                  Path to the Excel registration file");
    Console.WriteLine("\nOptions:");
    Console.WriteLine("  --timeslots <json-file>     Path to JSON file containing timeslot configuration");
    Console.WriteLine("  --no-merge-workshops        Disable merging of workshop cells in individual schedules");
    Console.WriteLine("  --blank-schedules <count>   Number of blank schedules to generate (for attendees not in roster)");
    Console.WriteLine("  --event-name <name>         Event name to display in PDF footer (default: \"Winter Adventure {current year}\")");
    return;
}

string filePath = args[0];
string? timeslotsPath = null;
bool mergeWorkshopCells = true;
int blankScheduleCount = 0;
string eventName = $"Winter Adventure {DateTime.Now.Year}";

// Parse arguments
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--timeslots" && i + 1 < args.Length)
    {
        timeslotsPath = args[i + 1];
        i++; // Skip next arg since we consumed it
    }
    else if (args[i] == "--no-merge-workshops")
    {
        mergeWorkshopCells = false;
    }
    else if (args[i] == "--blank-schedules" && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out int count) && count > 0)
        {
            blankScheduleCount = count;
            i++; // Skip next arg since we consumed it
        }
        else
        {
            Console.WriteLine($"Error: Invalid value for --blank-schedules: '{args[i + 1]}'. Must be a positive integer.");
            return;
        }
    }
    else if (args[i] == "--event-name" && i + 1 < args.Length)
    {
        eventName = args[i + 1];
        i++; // Skip next arg since we consumed it
    }
}

if (!File.Exists(filePath))
{
    Console.WriteLine($"Error: File '{filePath}' does not exist.");
    return;
}

try
{
    using (var stream = new MemoryStream(File.ReadAllBytes(filePath)))
    {
        var excelUtilities = new ExcelUtilities(logger);

        // Import and parse Excel
        Console.WriteLine($"Importing Excel file: {Path.GetFileName(filePath)}");
        excelUtilities.ImportExcel(stream);

        Console.WriteLine($"\n=== PARSED WORKSHOPS ===");
        foreach (var workshop in excelUtilities.Workshops)
        {
            Console.WriteLine($"\n{workshop.Name} ({workshop.Leader})");
            Console.WriteLine($"  Period: {workshop.Period.DisplayName}");
            Console.WriteLine($"  Duration: {workshop.Duration.Description}");
            Console.WriteLine($"  Participants: {workshop.Selections.Count}");
            Console.WriteLine($"    First choice: {workshop.Selections.Count(s => s.ChoiceNumber == 1)}");
            Console.WriteLine($"    Backup choices: {workshop.Selections.Count(s => s.ChoiceNumber > 1)}");
        }

        // Load timeslots if provided
        List<TimeSlot>? timeslots = null;
        if (!string.IsNullOrWhiteSpace(timeslotsPath))
        {
            timeslots = LoadTimeslots(timeslotsPath);
        }

        Console.WriteLine($"\n=== GENERATING PDF ===");
        Console.WriteLine($"Event name: {eventName}");
        Console.WriteLine($"Merge workshop cells: {mergeWorkshopCells}");
        Console.WriteLine($"Timeslots: {(timeslots == null ? "Using defaults" : $"{timeslots.Count} configured")}");
        Console.WriteLine($"Blank schedules: {blankScheduleCount}");
        var document = excelUtilities.CreatePdf(mergeWorkshopCells, timeslots, blankScheduleCount, eventName);

        if (document == null)
        {
            Console.WriteLine("Error: No document generated (no workshops found)");
            return;
        }

        GlobalFontSettings.FontResolver = new CustomFontResolver();

        var renderer = new PdfDocumentRenderer();
        renderer.Document = document;
        document!.Styles["Normal"]!.Font.Name = "NotoSans";

        Console.WriteLine("Rendering PDF document...");
        renderer.RenderDocument();

        var outputPath = Path.Combine(Path.GetDirectoryName(filePath) ?? ".", "ClassRosters.pdf");
        renderer.Save(outputPath);
        Console.WriteLine($"\n✓ SUCCESS: PDF saved to: {outputPath}");
    }
}
catch (InvalidDataException ex)
{
    Console.WriteLine($"\n✗ ERROR: Invalid data in Excel file");
    Console.WriteLine($"  {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"  Details: {ex.InnerException.Message}");
    }
    Environment.Exit(1);
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"\n✗ ERROR: File not found");
    Console.WriteLine($"  {ex.Message}");
    Environment.Exit(1);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"\n✗ ERROR: Failed to process Excel file");
    Console.WriteLine($"  {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"  Details: {ex.InnerException.Message}");
    }
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ UNEXPECTED ERROR: {ex.Message}");
    Console.WriteLine($"  Type: {ex.GetType().Name}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"  Inner Exception: {ex.InnerException.Message}");
    }
    Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
    Environment.Exit(1);
}

// Helper methods and DTOs

/// <summary>
/// Loads and validates timeslot configuration from a JSON file.
/// Ensures all period timeslots have configured times and no overlaps exist.
/// Exits with code 1 if file doesn't exist, JSON is invalid, or validation fails.
/// </summary>
/// <param name="timeslotsPath">Path to the JSON file containing timeslot definitions.</param>
/// <returns>A validated list of TimeSlot objects ready for PDF generation.</returns>
static List<TimeSlot> LoadTimeslots(string timeslotsPath)
{
    Console.WriteLine($"\nLoading timeslots from: {timeslotsPath}");

    if (!File.Exists(timeslotsPath))
    {
        Console.WriteLine($"✗ ERROR: Timeslots file '{timeslotsPath}' does not exist.");
        Environment.Exit(1);
    }

    try
    {
        var json = File.ReadAllText(timeslotsPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<TimeslotFileFormat>(json, options);
        if (data?.Timeslots == null || data.Timeslots.Count == 0)
        {
            Console.WriteLine("✗ ERROR: Invalid JSON structure - 'timeslots' array is empty or missing");
            Environment.Exit(1);
        }

        // Convert DTOs to Library TimeSlot models
        var timeslots = data.Timeslots.Select(t => new TimeSlot
        {
            Id = t.Id ?? Guid.NewGuid().ToString(),
            Label = t.Label ?? string.Empty,
            StartTime = t.StartTime,
            EndTime = t.EndTime,
            IsPeriod = t.IsPeriod
        }).ToList();

        // Validate timeslots
        var validator = new TimeslotValidationService();
        var timeslotDtos = timeslots.Select(t => new TimeSlotDto
        {
            Id = t.Id,
            Label = t.Label,
            StartTime = t.StartTime,
            EndTime = t.EndTime,
            IsPeriod = t.IsPeriod
        });

        var validationResult = validator.ValidateTimeslots(timeslotDtos);

        if (validationResult.HasUnconfiguredTimeslots)
        {
            Console.WriteLine("✗ ERROR: Some period timeslots are missing start or end times");
            Console.WriteLine("  Period timeslots must have both StartTime and EndTime configured");
            Environment.Exit(1);
        }

        if (validationResult.HasOverlappingTimeslots)
        {
            Console.WriteLine("✗ ERROR: Timeslots have overlapping times or duplicate start times");
            Console.WriteLine("  Please check your timeslot configuration and fix any conflicts");
            Environment.Exit(1);
        }

        // Display loaded timeslots
        Console.WriteLine($"Loaded {timeslots.Count} timeslots:");
        foreach (var ts in timeslots.OrderBy(t => t.StartTime ?? TimeSpan.MaxValue))
        {
            var timeRange = ts.StartTime.HasValue && ts.EndTime.HasValue
                ? $"({ts.TimeRange})"
                : "(no times configured)";
            var type = ts.IsPeriod ? "[Period]" : "[Activity]";
            Console.WriteLine($"  - {ts.Label} {timeRange} {type}");
        }

        return timeslots;
    }
    catch (JsonException ex)
    {
        Console.WriteLine("✗ ERROR: Invalid JSON format in timeslots file");
        Console.WriteLine($"  {ex.Message}");
        Environment.Exit(1);
        return null!; // Unreachable, but needed for compiler
    }
}

/// <summary>
/// Represents the root structure of a timeslots JSON configuration file.
/// Used for deserializing JSON into strongly-typed objects for CLI processing.
/// </summary>
class TimeslotFileFormat
{
    /// <summary>
    /// Gets or sets the collection of timeslot definitions.
    /// </summary>
    public List<TimeslotDto>? Timeslots { get; set; }
}

/// <summary>
/// Data transfer object for a single timeslot definition in JSON format.
/// Used during CLI JSON deserialization before conversion to Library TimeSlot model.
/// </summary>
class TimeslotDto
{
    /// <summary>
    /// Gets or sets the unique identifier for this timeslot. Auto-generated if not provided.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the display label (e.g., "Morning First Period", "Lunch").
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the start time of this timeslot.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of this timeslot.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this timeslot represents a workshop period.
    /// Period timeslots must have both StartTime and EndTime configured.
    /// </summary>
    public bool IsPeriod { get; set; }
}

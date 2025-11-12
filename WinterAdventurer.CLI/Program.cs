using MigraDoc.Rendering;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.Diagnostics;
using WinterAdventurer.Library;
using Microsoft.Extensions.Logging;

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
    Console.WriteLine("Usage: WinterAdventurer.CLI <excel-file> [--no-merge-workshops]");
    return;
}

string filePath = args[0];
bool mergeWorkshopCells = !args.Contains("--no-merge-workshops");

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

        Console.WriteLine($"\n=== GENERATING PDF ===");
        Console.WriteLine($"Merge workshop cells: {mergeWorkshopCells}");
        var document = excelUtilities.CreatePdf(mergeWorkshopCells);

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
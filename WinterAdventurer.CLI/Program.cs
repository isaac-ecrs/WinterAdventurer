using MigraDoc.Rendering;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.Diagnostics;
using WinterAdventurer.Library;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

if (args.Length == 0)
{
    Console.WriteLine("Please provide the path to the Excel file as an argument.");
    return;
}

string filePath = args[0];

if (!File.Exists(filePath))
{
    Console.WriteLine($"File {filePath} does not exist.");
    return;
}

using (var stream = new MemoryStream(File.ReadAllBytes(filePath)))
{
    var excelUtilities = new ExcelUtilities();

    // Import and parse Excel
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
    var document = excelUtilities.CreatePdf();

    GlobalFontSettings.FontResolver = new CustomFontResolver();

    var renderer = new PdfDocumentRenderer();
    renderer.Document = document;
    document.Styles["Normal"].Font.Name = "NotoSans";

    renderer.RenderDocument();

    var outputPath = Path.Combine(Path.GetDirectoryName(filePath) ?? ".", "ClassRosters.pdf");
    renderer.Save(outputPath);
    Console.WriteLine($"PDF saved to: {outputPath}");
}
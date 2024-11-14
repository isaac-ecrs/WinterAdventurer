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

    excelUtilities.ImportExcel(stream);

    foreach (var workshop in excelUtilities.Workshops)
    {
        Console.WriteLine(workshop.ToString());
    }    

    var document = excelUtilities.CreatePdf();

    GlobalFontSettings.FontResolver = new CustomFontResolver();

    var renderer = new PdfDocumentRenderer();
    renderer.Document = document;
    document.Styles["Normal"].Font.Name = "NotoSans-Medium";   

    renderer.RenderDocument();
    renderer.Save(filePath);
    //Process.Start(filename);
}
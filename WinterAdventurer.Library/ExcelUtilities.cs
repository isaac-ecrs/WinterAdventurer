using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using WinterAdventurer.Library.Extensions;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.EventSchemas;
using MigraDoc;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using System.Data.Common;
using System.Xml;
using PdfSharp.Fonts;
using System.Diagnostics;
using MigraDoc.DocumentObjectModel.Visitors;
using PdfSharp.Pdf;
using Newtonsoft.Json;

namespace WinterAdventurer.Library
{
    public class ExcelUtilities
    {
        public List<Workshop> Workshops = new List<Workshop>();
        
        readonly Color COLOR_BLACK = Color.FromRgb(0, 0, 0);

        public ExcelUtilities()
        {
            GlobalFontSettings.FontResolver = new CustomFontResolver();
        }

        private EventSchema LoadEventSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WinterAdventurer.Library.EventSchemas.WinterAdventureSchema.json";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not find embedded resource: {resourceName}");
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var schema = JsonConvert.DeserializeObject<EventSchema>(json);
                    if (schema == null)
                    {
                        throw new InvalidDataException("Failed to deserialize event schema");
                    }
                    return schema;
                }
            }
        }

        public void ImportExcel(Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            stream.Position = 0;

            using (var package = new ExcelPackage(stream))
            {
                Workshops.AddRange(ParseWorkshops(package));
            }
        }

        public void DumpExcelSchema(Stream stream, string outputPath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            stream.Position = 0;

            using (var package = new ExcelPackage(stream))
            {
                var schema = new
                {
                    WorksheetCount = package.Workbook.Worksheets.Count,
                    Worksheets = package.Workbook.Worksheets.Select(ws =>
                    {
                        var headers = ws.Dimension != null
                            ? Enumerable.Range(1, ws.Dimension.Columns)
                                .Select(i => new { Column = i, Value = ws.Cells[1, i].Value?.ToString() })
                                .Cast<object>()
                                .ToList()
                            : new List<object>();

                        var sampleRow = ws.Dimension != null
                            ? Enumerable.Range(1, ws.Dimension.Columns)
                                .Select(i => new { Column = i, Value = ws.Cells[2, i].Value?.ToString() })
                                .Cast<object>()
                                .ToList()
                            : new List<object>();

                        return new
                        {
                            Name = ws.Name,
                            Dimensions = ws.Dimension?.Address,
                            RowCount = ws.Dimension?.Rows,
                            ColumnCount = ws.Dimension?.Columns,
                            Headers = headers,
                            SampleRow = sampleRow
                        };
                    }).ToList()
                };

                var json = JsonConvert.SerializeObject(schema, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(outputPath, json);
                Console.WriteLine($"Excel schema written to: {outputPath}");
            }
        }

        public List<Workshop> ParseWorkshops(ExcelPackage package)
        {
            var schema = LoadEventSchema();
            Console.WriteLine($"Loaded schema for: {schema.EventName}");

            // Step 1: Load all attendees from ClassSelection sheet
            var attendees = LoadAttendees(package, schema);
            Console.WriteLine($"Loaded {attendees.Count} attendees");

            // Step 2: Parse workshops from each period sheet defined in schema
            var allWorkshops = new List<Workshop>();
            foreach (var periodConfig in schema.PeriodSheets)
            {
                var sheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == periodConfig.SheetName);
                if (sheet == null)
                {
                    Console.WriteLine($"Warning: Could not find sheet {periodConfig.SheetName}");
                    continue;
                }

                Console.WriteLine($"Processing sheet: {sheet.Name}");
                var workshops = CollectWorkshops(sheet, periodConfig, attendees);
                allWorkshops.AddRange(workshops);
                Console.WriteLine($"  Found {workshops.Count} workshops in {sheet.Name}");
            }

            Console.WriteLine($"Total workshops parsed: {allWorkshops.Count}");
            return allWorkshops;
        }

        private Dictionary<string, Attendee> LoadAttendees(ExcelPackage package, EventSchema schema)
        {
            var attendees = new Dictionary<string, Attendee>();
            var sheetConfig = schema.ClassSelectionSheet;
            var sheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetConfig.SheetName);

            if (sheet == null || sheet.Dimension == null)
            {
                Console.WriteLine($"Warning: Could not find {sheetConfig.SheetName} sheet");
                return attendees;
            }

            var helper = new SheetHelper(sheet);
            var rows = sheet.Dimension.Rows;

            for (int row = 2; row <= rows; row++)
            {
                var selectionId = helper.GetCellValueByPattern(row, sheetConfig.GetColumnName("selectionId"));
                var firstName = helper.GetCellValue(row, sheetConfig.GetColumnName("firstName"));
                var lastName = helper.GetCellValue(row, sheetConfig.GetColumnName("lastName"));
                var email = helper.GetCellValue(row, sheetConfig.GetColumnName("email"));
                var age = helper.GetCellValue(row, sheetConfig.GetColumnName("age"));

                // Skip rows with no name
                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                    continue;

                // Generate fallback ID if missing
                if (string.IsNullOrWhiteSpace(selectionId))
                {
                    selectionId = $"{firstName}{lastName}".Replace(" ", "");
                }

                attendees[selectionId] = new Attendee
                {
                    ClassSelectionId = selectionId,
                    FirstName = firstName ?? "",
                    LastName = lastName ?? "",
                    Email = email ?? "",
                    Age = age ?? ""
                };
            }

            return attendees;
        }

        private List<Workshop> CollectWorkshops(ExcelWorksheet sheet, PeriodSheetConfig periodConfig, Dictionary<string, Attendee> attendees)
        {
            if (sheet.Dimension == null) return new List<Workshop>();

            var helper = new SheetHelper(sheet);
            var period = new Period(periodConfig.SheetName);
            period.DisplayName = periodConfig.DisplayName;
            var workshops = new Dictionary<string, Workshop>();
            var rows = sheet.Dimension.Rows;

            for (int row = 2; row <= rows; row++)
            {
                var selectionId = helper.GetCellValueByPattern(row, periodConfig.GetColumnName("selectionId"));
                var choiceNumberStr = helper.GetCellValue(row, periodConfig.GetColumnName("choiceNumber"));
                var registrationIdStr = helper.GetCellValueByPattern(row, periodConfig.GetColumnName("registrationId"));

                if (!int.TryParse(choiceNumberStr, out int choiceNumber))
                    choiceNumber = 1;

                if (!int.TryParse(registrationIdStr, out int registrationId))
                    registrationId = 0;

                // Process each workshop column configured for this period
                foreach (var workshopCol in periodConfig.WorkshopColumns)
                {
                    var cellValue = helper.GetCellValue(row, workshopCol.ColumnName);
                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                    var workshopName = cellValue.GetWorkshopName();
                    var leaderName = cellValue.GetLeaderName();

                    if (string.IsNullOrWhiteSpace(workshopName)) continue;

                    // Find or create attendee
                    Attendee? attendee = null;
                    if (!string.IsNullOrWhiteSpace(selectionId) && attendees.ContainsKey(selectionId))
                    {
                        attendee = attendees[selectionId];
                    }
                    else
                    {
                        // Fallback: try to get name from the row
                        var firstName = helper.GetCellValue(row, periodConfig.GetColumnName("firstName")) ?? "";
                        var lastName = helper.GetCellValue(row, periodConfig.GetColumnName("lastName")) ?? "";
                        attendee = new Attendee
                        {
                            ClassSelectionId = selectionId ?? $"{firstName}{lastName}",
                            FirstName = firstName,
                            LastName = lastName
                        };
                    }

                    // Create workshop selection
                    var duration = new WorkshopDuration(workshopCol.StartDay, workshopCol.EndDay);
                    var selection = new WorkshopSelection
                    {
                        ClassSelectionId = attendee.ClassSelectionId,
                        WorkshopName = workshopName,
                        FirstName = attendee.FirstName,
                        LastName = attendee.LastName,
                        FullName = attendee.FullName,
                        ChoiceNumber = choiceNumber,
                        Duration = duration,
                        RegistrationId = registrationId
                    };

                    // Create unique key for this workshop offering
                    var workshopKey = $"{period.SheetName}|{workshopName}|{leaderName}|{duration.StartDay}-{duration.EndDay}";

                    // Add to workshops dictionary
                    if (workshops.TryGetValue(workshopKey, out var existingWorkshop))
                    {
                        existingWorkshop.Selections.Add(selection);
                    }
                    else
                    {
                        workshops[workshopKey] = new Workshop
                        {
                            Name = workshopName,
                            Leader = leaderName,
                            Period = period,
                            Duration = duration,
                            Selections = new List<WorkshopSelection> { selection }
                        };
                    }
                }
            }

            return workshops.Values.ToList();
        }

        public Document TestFromDocs()
        {
            // Create a new MigraDoc document.
            var document = new Document();

            // Add a section to the document.
            var section = document.AddSection();

            // Add a paragraph to the section.
            var paragraph = section.AddParagraph();

            // Set font color.
            paragraph.Format.Font.Color = Colors.DarkBlue;

            // Add some text to the paragraph.
            paragraph.AddFormattedText("Hello, World!", TextFormat.Bold);

            // Create the primary footer.
            var footer = section.Footers.Primary;

            // Add content to footer.
            paragraph = footer.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Center;

            // Add MigraDoc logo.
            document.LastSection.AddParagraph("blah");

            return document;
        }

        public Document TestPdf()
        {
            // Create a new MigraDoc document.
            var document = new Document();

            // Add a section to the document.
            var section = document.AddSection();

            // Add a paragraph to the section.
            var paragraph = section.AddParagraph();

            // Set font color.
            // paragraph.Format.Font.Color = Colors.DarkBlue;

            // Add some text to the paragraph.
            paragraph.AddFormattedText("Hello, World! No footer at all", TextFormat.Bold);

            // Add MigraDoc logo.
            // document.LastSection.AddParagraph("blah");

            return document;
        }

        public Document CreatePdf()
        {
            if (Workshops != null)
            {
                var document = new Document();

                foreach(var section in PrintWorkshopParticipants())
                {
                    section.PageSetup.TopMargin = Unit.FromInch(.5);
                    section.PageSetup.LeftMargin = Unit.FromInch(.5);
                    section.PageSetup.RightMargin = Unit.FromInch(.5);
                    section.PageSetup.BottomMargin = Unit.FromInch(.5);

                    document.Sections.Add(section);
                }

                // TODO: Add individual schedule generation
                // foreach(var section in PrintSchedules())
                // {
                //     document.Sections.Add(section);
                // }

                return document;
            }

            return null;
        }

        private List<Section> PrintWorkshopParticipants()
        {
            var sections = new List<Section>();

            foreach(var workshopListing in Workshops)
            {
                var section = new Section();

                // Workshop name - Oswald (top level header)
                var header = section.AddParagraph();
                header.Format.Font.Name = "Oswald";
                header.Format.Font.Color = COLOR_BLACK;
                header.Format.Font.Size = 25;
                header.Format.Alignment = ParagraphAlignment.Center;
                header.AddFormattedText(workshopListing.Name, TextFormat.Bold);

                // Leader info - Noto Sans (second level header)
                var leaderInfo = section.AddParagraph();
                leaderInfo.Format.Font.Name = "NotoSans";
                leaderInfo.Format.Font.Color = COLOR_BLACK;
                leaderInfo.Format.Font.Size = 18;
                leaderInfo.Format.Alignment = ParagraphAlignment.Center;
                leaderInfo.AddFormattedText(workshopListing.Leader);

                // Period and duration info - Noto Sans (second level header)
                var periodInfo = section.AddParagraph();
                periodInfo.Format.Font.Name = "NotoSans";
                periodInfo.Format.Font.Color = COLOR_BLACK;
                periodInfo.Format.Font.Size = 14;
                periodInfo.Format.Alignment = ParagraphAlignment.Center;
                periodInfo.AddFormattedText($"{workshopListing.Period.DisplayName} - {workshopListing.Duration.Description}");
                periodInfo.Format.SpaceAfter = Unit.FromPoint(16);

                // Separate first choice from backup choices and sort by registration order
                var firstChoiceAttendees = workshopListing.Selections
                    .Where(s => s.ChoiceNumber == 1)
                    .OrderBy(s => s.RegistrationId)
                    .ToList();
                var backupAttendees = workshopListing.Selections
                    .Where(s => s.ChoiceNumber > 1)
                    .OrderBy(s => s.RegistrationId)
                    .ThenBy(s => s.ChoiceNumber)
                    .ToList();

                // First choice participants
                if (firstChoiceAttendees.Any())
                {
                    var firstChoiceHeader = section.AddParagraph();
                    firstChoiceHeader.Format.Font.Name = "NotoSans";
                    firstChoiceHeader.Format.Font.Color = COLOR_BLACK;
                    firstChoiceHeader.Format.Font.Size = 18;
                    firstChoiceHeader.AddFormattedText($"Enrolled Participants ({firstChoiceAttendees.Count}):", TextFormat.Bold);
                    firstChoiceHeader.Format.SpaceAfter = Unit.FromPoint(8);

                    AddTwoColumnParticipantList(section, firstChoiceAttendees, showChoiceNumber: false);
                }

                // Backup participants
                if (backupAttendees.Any())
                {
                    var backupHeader = section.AddParagraph();
                    backupHeader.Format.Font.Name = "NotoSans";
                    backupHeader.Format.Font.Color = COLOR_BLACK;
                    backupHeader.Format.Font.Size = 18;
                    backupHeader.Format.SpaceAfter = Unit.FromPoint(8);
                    backupHeader.Format.SpaceBefore = Unit.FromPoint(16);
                    backupHeader.AddFormattedText($"Backup/Alternate Choices ({backupAttendees.Count}):", TextFormat.Bold);

                    var backupNote = section.AddParagraph();
                    backupNote.Format.Font.Name = "Roboto";
                    backupNote.Format.Font.Color = COLOR_BLACK;
                    backupNote.Format.Font.Size = 14;
                    backupNote.Format.Font.Italic = true;
                    backupNote.Format.LeftIndent = Unit.FromPoint(12);
                    backupNote.Format.SpaceAfter = Unit.FromPoint(8);
                    backupNote.AddText("These participants may join if their first choice is full:");

                    AddTwoColumnParticipantList(section, backupAttendees, showChoiceNumber: true);
                }

                sections.Add(section);
            }

            return sections;
        }

        private int GetParticipantFontSize(string fullName, int counter, bool showChoiceNumber, int? choiceNumber = null)
        {
            // Calculate full text length including number, name, choice indicator, and checkbox
            string text = $"{counter}. {fullName}";
            if (showChoiceNumber && choiceNumber.HasValue)
            {
                text += $" (Choice #{choiceNumber})";
            }
            text += " [\u2003]";  // Using em space for wider checkbox

            // Use smaller font for longer names to prevent wrapping
            return text.Length > 30 ? 13 : 15;
        }

        private void AddTwoColumnParticipantList(Section section, List<WorkshopSelection> participants, bool showChoiceNumber)
        {
            // Create a table with 2 columns
            var table = section.AddTable();
            table.Borders.Visible = false;

            // Define column widths (equal width)
            var columnWidth = Unit.FromInch(3.25); // Roughly half of page width with margins
            table.AddColumn(columnWidth);
            table.AddColumn(columnWidth);

            // Split participants into two columns
            int halfCount = (int)Math.Ceiling(participants.Count / 2.0);
            var leftColumn = participants.Take(halfCount).ToList();
            var rightColumn = participants.Skip(halfCount).ToList();

            // Add rows (one row per participant in left column)
            for (int i = 0; i < leftColumn.Count; i++)
            {
                var row = table.AddRow();
                row.VerticalAlignment = VerticalAlignment.Top;
                row.TopPadding = 0;
                row.BottomPadding = 0;

                // Left column participant
                var leftCell = row.Cells[0];
                leftCell.VerticalAlignment = VerticalAlignment.Top;
                var leftPara = leftCell.AddParagraph();
                leftPara.Format.Font.Name = "Roboto";
                leftPara.Format.Font.Color = COLOR_BLACK;

                int leftCounter = i + 1;
                int leftFontSize = GetParticipantFontSize(
                    leftColumn[i].FullName,
                    leftCounter,
                    showChoiceNumber,
                    leftColumn[i].ChoiceNumber);

                leftPara.Format.Font.Size = leftFontSize;
                leftPara.Format.LeftIndent = Unit.FromPoint(6);
                leftPara.Format.SpaceBefore = 0;
                leftPara.Format.SpaceAfter = Unit.FromPoint(6);
                leftPara.Format.LineSpacingRule = LineSpacingRule.Exactly;
                leftPara.Format.LineSpacing = Unit.FromPoint(leftFontSize);

                leftPara.AddFormattedText($"{leftCounter}. ", TextFormat.Bold);
                leftPara.AddText(leftColumn[i].FullName);
                if (showChoiceNumber)
                {
                    leftPara.AddText($" (Choice #{leftColumn[i].ChoiceNumber})");
                }
                leftPara.AddText(" [\u2003]");

                // Right column participant (if exists)
                if (i < rightColumn.Count)
                {
                    var rightCell = row.Cells[1];
                    rightCell.VerticalAlignment = VerticalAlignment.Top;
                    var rightPara = rightCell.AddParagraph();
                    rightPara.Format.Font.Name = "Roboto";
                    rightPara.Format.Font.Color = COLOR_BLACK;

                    int rightCounter = halfCount + i + 1;
                    int rightFontSize = GetParticipantFontSize(
                        rightColumn[i].FullName,
                        rightCounter,
                        showChoiceNumber,
                        rightColumn[i].ChoiceNumber);

                    rightPara.Format.Font.Size = rightFontSize;
                    rightPara.Format.LeftIndent = Unit.FromPoint(6);
                    rightPara.Format.SpaceBefore = 0;
                    rightPara.Format.SpaceAfter = Unit.FromPoint(6);
                    rightPara.Format.LineSpacingRule = LineSpacingRule.Exactly;
                    rightPara.Format.LineSpacing = Unit.FromPoint(rightFontSize);

                    rightPara.AddFormattedText($"{rightCounter}. ", TextFormat.Bold);
                    rightPara.AddText(rightColumn[i].FullName);
                    if (showChoiceNumber)
                    {
                        rightPara.AddText($" (Choice #{rightColumn[i].ChoiceNumber})");
                    }
                    rightPara.AddText(" [\u2003]");
                }
            }
        }

        // @TODO switch from hardcoded workshops to those from the uploaded file
        private Table BuildScheduleTemplate(Section section)
        {
            section.PageSetup.Orientation = Orientation.Landscape;

            var scheduleTable = section.AddTable();

            scheduleTable.Borders.Width = .75;

            string[] headers = { "Period", "Days", "Dining Room", "Martin Room", "Chapel A", "Library", "Rec Hall", "Craft Room", "Elm Room" };

            foreach (string header in headers)
            {
                Column column = scheduleTable.AddColumn(Unit.FromCentimeter(3)); // Adjust the column width as needed
                column.Format.Alignment = ParagraphAlignment.Center;
            }

            Row headerRow = scheduleTable.AddRow();
            
            for(int i = 0; i < headers.Length; i++)
            {
                AddTextToCell(headerRow.Cells[i], headers[i]);
                headerRow.Cells[i].Format.Font.Bold = true;
                headerRow.Cells[i].Shading.Color = Colors.LightGray;
            }            

            var row1 = scheduleTable.AddRow();
            var row2 = scheduleTable.AddRow();
            var row3 = scheduleTable.AddRow();
            var row4 = scheduleTable.AddRow();
            var row5 = scheduleTable.AddRow();
            var row6 = scheduleTable.AddRow();

            // Row 1
            AddTextToCell(row1.Cells[0], "1st Period");
            row1.Cells[0].MergeDown = 1;

            AddTextToCell(row1.Cells[1], "Days 1-2");

            AddTextToCell(row1.Cells[2], "International Folk Dancing (Patricia)");
            row1.Cells[2].MergeDown = 1;

            AddTextToCell(row1.Cells[3], "Wood Carving (Steve)");
            row1.Cells[3].MergeDown = 1;

            AddTextToCell(row1.Cells[4], "Sing and Play Instruments (Bonnie)");
            AddTextToCell(row1.Cells[5], "");
            AddTextToCell(row1.Cells[6], "Storytelling (Frank)");
            AddTextToCell(row1.Cells[7], "");

            AddTextToCell(row1.Cells[8], "Children's Program");
            row1.Cells[8].MergeDown = 1;

            // Row 2
            AddTextToCell(row2.Cells[1], "Days 3-4");

            AddTextToCell(row2.Cells[4], "A Capella Songs from the Sea and the City (Mark)");

            AddTextToCell(row2.Cells[6], "Informal Dramatics (Lane)");

            // Row 3
            AddTextToCell(row3.Cells[0], "2nd Period");
            row3.Cells[0].MergeDown = 1;

            AddTextToCell(row3.Cells[1], "Days 1-2");

            AddTextToCell(row3.Cells[2], "Late Night Dance");
            AddTextToCell(row3.Cells[4], "Breakthrough Decluttering (Bari)");
            AddTextToCell(row3.Cells[5], "The Write Stuff (Beverly)");
            AddTextToCell(row3.Cells[6], "Improv Theater (Howard)");

            AddTextToCell(row3.Cells[8], "Cooperative Children's Program");
            row3.Cells[8].MergeDown = 1;

            // Row 4
            AddTextToCell(row4.Cells[1], "Days 3-4");

            AddTextToCell(row4.Cells[2], "Scottish Dance (Patricia)");
            AddTextToCell(row4.Cells[4], "A Journey to Self-Kindness (Heather)");
            AddTextToCell(row4.Cells[5], "The Write Stuff (Beverly)");
            AddTextToCell(row4.Cells[6], "Party Games for All Ages (Trevor)");

            // Row 5
            AddTextToCell(row5.Cells[0], "3rd Period");
            row5.Cells[0].MergeDown = 1;

            AddTextToCell(row5.Cells[1], "Days 1-2");

            AddTextToCell(row5.Cells[2], "Dymanic Group Leadership (Lane)");

            AddTextToCell(row5.Cells[4], "Small Scenes (Glenn & Isaac)");
            row5.Cells[4].MergeDown = 1;

            AddTextToCell(row5.Cells[6], "Active Games for Teens and Adults (Trevor)");

            AddTextToCell(row5.Cells[7], "Posters with Wings (Teri)");
            row5.Cells[7].MergeDown = 1;

            AddTextToCell(row5.Cells[8], "Children's Program");
            row5.Cells[8].MergeDown = 1;

            // Row 6
            AddTextToCell(row6.Cells[1], "Days 3-4");

            AddTextToCell(row6.Cells[2], "Collaborative Board Games (Max)");
            AddTextToCell(row6.Cells[3], "The Children's March (Carolyn)");
            AddTextToCell(row6.Cells[6], "The Joy of Movement (Judi)");

            return scheduleTable;
        }

        private void AddTextToCell(Cell cell, string text)
        {
            var paragraph = cell.AddParagraph();
            paragraph.AddText(text);
        }

       private List<Section> PrintSchedules()
        {
            var sections = new List<Section>();

            foreach(var person in CollectSelectionsByAttendee())
            {
                var section = new Section();

                BuildScheduleTemplate(section);

                //var formattedSelection = section.AddParagraph();
                //formattedSelection.Format.Font.Color = COLOR_BLACK;
                //formattedSelection.AddFormattedText(person.Key + ':' + PrintClassSelections(person.Value));

                sections.Add(section);
            }

            return sections;
        }

        private string PrintClassSelections(List<WorkshopSelection> selections)
        {
            var result = "\n";

            foreach(var selection in selections)
            {
                result += selection.WorkshopName + '\n';
            }

            return result;
        }

        private int GetColumnIndex(ExcelWorksheet sheet, string columnName) 
        {
            return sheet.Cells["1:1"].First(cell => 
                cell.Value.ToString()
                    .Contains(columnName))
                    .Start.Column;
        }

        private Dictionary<string, List<WorkshopSelection>> CollectSelectionsByAttendee()
        {
            var people = new Dictionary<string, List<WorkshopSelection>>();

            foreach (var workshop in Workshops)
            {
                foreach(var selection in workshop.Selections)
                {
                    people.TryAdd(selection.FullName, new List<WorkshopSelection>());
                }
            }

            foreach (var person in people)
            {
                foreach (var workshop in Workshops)
                {
                    person.Value.AddRange(workshop.Selections.Where(s => s.FullName == person.Key));
                }
            }

            return people;
        }

        // OBSOLETE: This method is no longer used. We now use LoadAttendees with schema.
        private List<WorkshopSelection> ParseClassSelections(ExcelPackage package)
        {
            var classSelectionsSheet = package
                                            .Workbook
                                            .Worksheets
                                            .FirstOrDefault(ws =>
                                                    ws.Name == "ClassSelection");

            if (classSelectionsSheet == null || classSelectionsSheet == default)
            {
                throw new InvalidDataException("Could not find the Class Selection worksheet.");
            }

            var rows = classSelectionsSheet.Dimension.Rows;
            var selections = new List<WorkshopSelection>();

            for (int i = 2; i <= rows; i++)
            {
                var selectionId = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "ClassSelection_Id").Start.Column].Value?.ToString() ?? "";
                var firstName = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "Name_First").Start.Column].Value?.ToString() ?? "";
                var lastName = classSelectionsSheet.Cells[i, classSelectionsSheet.Cells["1:1"].First(cell => cell.Value.ToString() == "Name_Last").Start.Column].Value?.ToString() ?? "";

                selections.Add(
                    new WorkshopSelection()
                    {
                        ClassSelectionId = selectionId,
                        FirstName = firstName,
                        LastName = lastName,
                        WorkshopName = "", // Not available in this context
                        ChoiceNumber = 1,
                        Duration = new WorkshopDuration(1, 1)
                    }
                );
            };

            return selections;
        }
    }
}

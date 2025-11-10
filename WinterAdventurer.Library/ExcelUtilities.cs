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
using MigraDoc.DocumentObjectModel.Shapes;
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
        private EventSchema? _schema;

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
            _schema = LoadEventSchema();
            Console.WriteLine($"Loaded schema for: {_schema.EventName}");

            // Step 1: Load all attendees from ClassSelection sheet
            var attendees = LoadAttendees(package, _schema);
            Console.WriteLine($"Loaded {attendees.Count} attendees");

            // Step 2: Parse workshops from each period sheet defined in schema
            var allWorkshops = new List<Workshop>();
            foreach (var periodConfig in _schema.PeriodSheets)
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

        public Document CreatePdf(bool mergeWorkshopCells = true, List<Models.TimeSlot>? timeslots = null)
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

                // Add individual schedules
                foreach(var section in PrintIndividualSchedules(mergeWorkshopCells, timeslots))
                {
                    section.PageSetup.TopMargin = Unit.FromInch(.5);
                    section.PageSetup.LeftMargin = Unit.FromInch(.5);
                    section.PageSetup.RightMargin = Unit.FromInch(.5);
                    section.PageSetup.BottomMargin = Unit.FromInch(.5);

                    document.Sections.Add(section);
                }

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

                // Location info - Noto Sans
                if (!string.IsNullOrWhiteSpace(workshopListing.Location))
                {
                    var locationInfo = section.AddParagraph();
                    locationInfo.Format.Font.Name = "NotoSans";
                    locationInfo.Format.Font.Color = COLOR_BLACK;
                    locationInfo.Format.Font.Size = 16;
                    locationInfo.Format.Alignment = ParagraphAlignment.Center;
                    locationInfo.AddFormattedText($"Location: {workshopListing.Location}");
                }

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

        private List<Section> PrintIndividualSchedules(bool mergeWorkshopCells = true, List<Models.TimeSlot>? timeslots = null)
        {
            var sections = new List<Section>();

            if (_schema == null)
            {
                Console.WriteLine("Warning: Cannot generate individual schedules - schema not loaded");
                return sections;
            }

            // If no timeslots provided, create default minimal set
            if (timeslots == null || !timeslots.Any())
            {
                timeslots = CreateDefaultTimeslots();
            }

            // Get all unique attendees from workshop selections (first choice only)
            var attendeeSchedules = new Dictionary<string, List<WorkshopSelection>>();

            foreach (var workshop in Workshops)
            {
                foreach (var selection in workshop.Selections.Where(s => s.ChoiceNumber == 1))
                {
                    if (!attendeeSchedules.ContainsKey(selection.ClassSelectionId))
                    {
                        attendeeSchedules[selection.ClassSelectionId] = new List<WorkshopSelection>();
                    }
                    attendeeSchedules[selection.ClassSelectionId].Add(selection);
                }
            }

            // Create a schedule page for each attendee
            foreach (var kvp in attendeeSchedules.OrderBy(a => a.Value.First().LastName).ThenBy(a => a.Value.First().FirstName))
            {
                var attendeeSelections = kvp.Value;
                var attendee = attendeeSelections.First(); // Get attendee info from first selection

                var section = new Section();

                // Set landscape orientation for individual schedules
                section.PageSetup.Orientation = Orientation.Landscape;

                // Set margins for landscape pages
                section.PageSetup.LeftMargin = Unit.FromInch(0.5);
                section.PageSetup.RightMargin = Unit.FromInch(0.5);
                section.PageSetup.TopMargin = Unit.FromInch(0.5);
                section.PageSetup.BottomMargin = Unit.FromInch(0.5);

                // Header with attendee name - Oswald
                var header = section.AddParagraph();
                header.Format.Font.Name = "Oswald";
                header.Format.Font.Color = COLOR_BLACK;
                header.Format.Font.Size = 30;
                header.Format.Alignment = ParagraphAlignment.Center;
                header.AddFormattedText($"{attendee.FullName}'s Schedule", TextFormat.Bold);
                header.Format.SpaceAfter = Unit.FromPoint(16);

                // Create a TextFrame to contain the table with explicit positioning
                double tableWidth = 10.0; // 2.0 + (4 * 2.0) - fills the entire usable width
                double pageWidth = 11.0 - 1.0; // landscape width minus combined margins
                double leftPosition = (pageWidth - tableWidth) / 2.0;

                var frame = section.AddTextFrame();
                frame.Left = Unit.FromInch(leftPosition);
                frame.RelativeHorizontal = RelativeHorizontal.Margin;
                frame.Top = Unit.FromInch(0);
                frame.RelativeVertical = RelativeVertical.Paragraph;
                frame.Width = Unit.FromInch(tableWidth);

                // Create schedule table inside the frame
                var table = frame.AddTable();
                table.Borders.Width = 0.5;

                // Column 0: Period labels (time column)
                table.AddColumn(Unit.FromInch(2.0));

                // Columns 1-4: Days
                for (int i = 0; i < _schema.TotalDays; i++)
                {
                    table.AddColumn(Unit.FromInch(2.0));
                }

                // Header row with day numbers
                var headerRow = table.AddRow();
                headerRow.Shading.Color = Color.FromRgb(240, 240, 240);
                headerRow.HeadingFormat = true;

                var periodHeaderCell = headerRow.Cells[0];
                var periodHeaderPara = periodHeaderCell.AddParagraph();
                periodHeaderPara.Format.Font.Name = "NotoSans";
                periodHeaderPara.Format.Font.Bold = true;
                periodHeaderPara.Format.Font.Size = 18;
                periodHeaderPara.Format.Alignment = ParagraphAlignment.Center;
                periodHeaderPara.AddText("Time");

                for (int day = 1; day <= _schema.TotalDays; day++)
                {
                    var dayCell = headerRow.Cells[day];
                    var dayPara = dayCell.AddParagraph();
                    dayPara.Format.Font.Name = "NotoSans";
                    dayPara.Format.Font.Bold = true;
                    dayPara.Format.Font.Size = 18;
                    dayPara.Format.Alignment = ParagraphAlignment.Center;
                    dayPara.AddText($"Day {day}");
                }

                // Add rows dynamically based on timeslots
                foreach (var timeslot in timeslots)
                {
                    if (timeslot.IsPeriod)
                    {
                        // Find the matching period from schema
                        var periodConfig = _schema.PeriodSheets.FirstOrDefault(p =>
                            p.DisplayName.Equals(timeslot.Label, StringComparison.OrdinalIgnoreCase));

                        if (periodConfig == null)
                        {
                            // If we can't find a matching period, skip this timeslot
                            continue;
                        }

                        var row = table.AddRow();
                        row.VerticalAlignment = VerticalAlignment.Top;

                        // Time cell (first column) - only show time range, no label
                        var timeCell = row.Cells[0];
                        if (!string.IsNullOrWhiteSpace(timeslot.TimeRange))
                        {
                            var timePara = timeCell.AddParagraph();
                            timePara.Format.Font.Name = "Roboto";
                            timePara.Format.Font.Size = 12;
                            timePara.Format.Alignment = ParagraphAlignment.Center;
                            timePara.AddText(timeslot.TimeRange);
                        }

                        // Build a map of day -> workshop for this period
                        var dayWorkshopMap = new Dictionary<int, (Workshop? workshop, bool isLeading)>();
                        for (int day = 1; day <= _schema.TotalDays; day++)
                        {
                            // Find workshop they're enrolled in for this period and day
                            var workshopForDay = attendeeSelections
                                .Where(s => s.Duration.StartDay <= day && s.Duration.EndDay >= day)
                                .Select(s => Workshops.FirstOrDefault(w =>
                                    w.Name == s.WorkshopName &&
                                    w.Duration.StartDay == s.Duration.StartDay &&
                                    w.Duration.EndDay == s.Duration.EndDay &&
                                    w.Period.SheetName == periodConfig.SheetName))
                                .FirstOrDefault(w => w != null);

                            // Also check if they're leading a workshop in this period/day
                            var leadingWorkshop = Workshops.FirstOrDefault(w =>
                                w.Leader.Contains(attendee.FullName) &&
                                w.Period.SheetName == periodConfig.SheetName &&
                                w.Duration.StartDay <= day &&
                                w.Duration.EndDay >= day);

                            // Prefer the workshop they're leading if both exist
                            var workshopToShow = leadingWorkshop ?? workshopForDay;
                            bool isLeading = leadingWorkshop != null;

                            dayWorkshopMap[day] = (workshopToShow, isLeading);
                        }

                        // Process days and merge cells for consecutive same workshops
                        int currentDay = 1;
                        while (currentDay <= _schema.TotalDays)
                        {
                            var (workshop, isLeading) = dayWorkshopMap[currentDay];

                            if (workshop != null)
                            {
                                // Find how many consecutive days have the same workshop
                                int spanDays = 1;
                                for (int nextDay = currentDay + 1; nextDay <= _schema.TotalDays; nextDay++)
                                {
                                    var (nextWorkshop, nextIsLeading) = dayWorkshopMap[nextDay];
                                    if (nextWorkshop != null &&
                                        nextWorkshop.Name == workshop.Name &&
                                        nextWorkshop.Leader == workshop.Leader &&
                                        nextIsLeading == isLeading)
                                    {
                                        spanDays++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                // Get the cell and merge if spanning multiple days
                                var dayCell = row.Cells[currentDay];
                                if (mergeWorkshopCells && spanDays > 1)
                                {
                                    dayCell.MergeRight = spanDays - 1;
                                }

                                // Add workshop content
                                var workshopPara = dayCell.AddParagraph();
                                workshopPara.Format.Font.Size = 17;
                                workshopPara.Format.Alignment = ParagraphAlignment.Center;

                                if (isLeading)
                                {
                                    workshopPara.Format.Font.Name = "NotoSans";
                                    workshopPara.Format.Font.Bold = true;
                                    workshopPara.AddText($"Leading {workshop.Name}");
                                }
                                else
                                {
                                    workshopPara.Format.Font.Name = "Roboto";
                                    workshopPara.AddText($"{workshop.Name}");
                                }

                                // Add leader info
                                if (isLeading)
                                {
                                    // Check if co-leading (leader field contains "and")
                                    if (workshop.Leader.Contains(" and "))
                                    {
                                        // Extract the other leader's name
                                        var leaders = workshop.Leader.Split(new[] { " and " }, StringSplitOptions.None);
                                        var otherLeader = leaders.FirstOrDefault(l => l.Trim() != attendee.FullName)?.Trim();

                                        if (!string.IsNullOrEmpty(otherLeader))
                                        {
                                            var leaderPara = dayCell.AddParagraph();
                                            leaderPara.Format.Font.Name = "Roboto";
                                            leaderPara.Format.Font.Size = 12;
                                            leaderPara.Format.Font.Italic = true;
                                            leaderPara.Format.Alignment = ParagraphAlignment.Center;
                                            leaderPara.AddText($"with {otherLeader}");
                                        }
                                    }
                                    // If solo leader, don't show anything
                                }
                                else
                                {
                                    // Not leading, show full leader info
                                    var leaderPara = dayCell.AddParagraph();
                                    leaderPara.Format.Font.Name = "Roboto";
                                    leaderPara.Format.Font.Size = 12;
                                    leaderPara.Format.Font.Italic = true;
                                    leaderPara.Format.Alignment = ParagraphAlignment.Center;
                                    leaderPara.AddText($"({workshop.Leader})");
                                }

                                // Add location info
                                if (!string.IsNullOrWhiteSpace(workshop.Location))
                                {
                                    var locationPara = dayCell.AddParagraph();
                                    locationPara.Format.Font.Name = "Roboto";
                                    locationPara.Format.Font.Size = 12;
                                    locationPara.Format.Font.Bold = true;
                                    locationPara.Format.Alignment = ParagraphAlignment.Center;
                                    locationPara.AddText(workshop.Location);
                                }

                                // If merging, skip the merged days; otherwise just move to next day
                                currentDay += mergeWorkshopCells ? spanDays : 1;
                            }
                            else
                            {
                                currentDay++;
                            }
                        }
                    }
                    else
                    {
                        // Non-period activity (Breakfast, Lunch, etc.)
                        AddMergedActivityRow(table, timeslot.Label, _schema.TotalDays, timeslot.TimeRange);
                    }
                }

                sections.Add(section);
            }

            return sections;
        }

        private void AddMergedActivityRow(Table table, string activityName, int totalDays, string timeRange = "")
        {
            var row = table.AddRow();
            row.Shading.Color = Color.FromRgb(250, 250, 250);

            // First cell: time range (not merged)
            var timeCell = row.Cells[0];
            if (!string.IsNullOrWhiteSpace(timeRange))
            {
                var timePara = timeCell.AddParagraph();
                timePara.Format.Font.Name = "Roboto";
                timePara.Format.Font.Size = 12;
                timePara.Format.Alignment = ParagraphAlignment.Center;
                timePara.AddText(timeRange);
            }

            // Second cell: activity name merged across all day columns
            var activityCell = row.Cells[1];
            activityCell.MergeRight = totalDays - 1; // Merge across day columns only

            var para = activityCell.AddParagraph();
            para.Format.Font.Name = "Roboto";
            para.Format.Font.Size = 20;
            para.Format.Font.Italic = true;
            para.Format.Alignment = ParagraphAlignment.Center;
            para.AddFormattedText(activityName, TextFormat.Bold);
        }

        private List<Models.TimeSlot> CreateDefaultTimeslots()
        {
            var timeslots = new List<Models.TimeSlot>();

            // Add periods from schema
            if (_schema != null)
            {
                foreach (var period in _schema.PeriodSheets)
                {
                    timeslots.Add(new Models.TimeSlot
                    {
                        Label = period.DisplayName,
                        IsPeriod = true
                    });
                }
            }

            // Add default non-period activities
            timeslots.Insert(0, new Models.TimeSlot
            {
                Label = "Breakfast",
                IsPeriod = false
            });

            timeslots.Add(new Models.TimeSlot
            {
                Label = "Lunch",
                IsPeriod = false
            });

            timeslots.Add(new Models.TimeSlot
            {
                Label = "Evening Program",
                IsPeriod = false
            });

            return timeslots;
        }
    }
}

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
using Microsoft.Extensions.Logging;

namespace WinterAdventurer.Library
{
    public class ExcelUtilities
    {
        public List<Workshop> Workshops = new List<Workshop>();
        private EventSchema? _schema;
        private readonly ILogger<ExcelUtilities> _logger;

        readonly Color COLOR_BLACK = Color.FromRgb(0, 0, 0);

        public ExcelUtilities(ILogger<ExcelUtilities> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            GlobalFontSettings.FontResolver = new CustomFontResolver();
        }

        /// <summary>
        /// Loads the event schema configuration from embedded JSON resource.
        /// The schema defines Excel column mappings and workshop parsing rules for the event.
        /// </summary>
        /// <returns>Deserialized EventSchema object containing parsing configuration.</returns>
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

        /// <summary>
        /// Imports workshop registration data from an Excel file stream.
        /// Parses attendee information and workshop selections, populating the Workshops collection.
        /// </summary>
        /// <param name="stream">Excel file stream to import. Stream will be reset to position 0.</param>
        public void ImportExcel(Stream stream)
        {
            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("WinterAdventurer");

                if (stream == null || stream.Length == 0)
                {
                    throw new ArgumentException("Excel file stream is empty or null");
                }

                stream.Position = 0;

                _logger.LogInformation("Starting Excel import, stream size: {Size} bytes", stream.Length);

                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        throw new InvalidDataException("Excel file contains no worksheets");
                    }

                    _logger.LogDebug("Excel package loaded with {Count} worksheets", package.Workbook.Worksheets.Count);
                    Workshops.AddRange(ParseWorkshops(package));
                }

                _logger.LogInformation("Excel import completed successfully, {Count} workshops parsed", Workshops.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import Excel file");
                throw new InvalidOperationException("Failed to import Excel file. Please verify the file format matches the expected schema.", ex);
            }
        }

        /// <summary>
        /// Exports Excel file structure to JSON for debugging and schema development.
        /// Useful for understanding new Excel file formats before creating event schemas.
        /// </summary>
        /// <param name="stream">Excel file stream to analyze. Stream will be reset to position 0.</param>
        /// <param name="outputPath">File path where JSON schema dump will be written.</param>
        public void DumpExcelSchema(Stream stream, string outputPath)
        {
            ExcelPackage.License.SetNonCommercialOrganization("WinterAdventurer");
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
                _logger.LogInformation("Excel schema written to: {OutputPath}", outputPath);
            }
        }

        /// <summary>
        /// Parses all workshops from Excel package using event schema configuration.
        /// Loads attendees from ClassSelection sheet and processes each period sheet to build workshop roster.
        /// </summary>
        /// <param name="package">EPPlus ExcelPackage containing workshop registration data.</param>
        /// <returns>List of Workshop objects with associated participant selections.</returns>
        public List<Workshop> ParseWorkshops(ExcelPackage package)
        {
            try
            {
                _schema = LoadEventSchema();
                _logger.LogInformation("Loaded schema for event: {EventName}", _schema.EventName);

                // Step 1: Load all attendees from ClassSelection sheet
                var attendees = LoadAttendees(package, _schema);
                _logger.LogInformation("Loaded {Count} attendees from ClassSelection sheet", attendees.Count);

                if (attendees.Count == 0)
                {
                    _logger.LogWarning("No attendees found in ClassSelection sheet - workshop parsing may be incomplete");
                }

                // Step 2: Parse workshops from each period sheet defined in schema
                var allWorkshops = new List<Workshop>();
                foreach (var periodConfig in _schema.PeriodSheets)
                {
                    var sheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == periodConfig.SheetName);
                    if (sheet == null)
                    {
                        _logger.LogWarning("Could not find period sheet: {SheetName}", periodConfig.SheetName);
                        continue;
                    }

                    _logger.LogDebug("Processing period sheet: {SheetName}", sheet.Name);
                    var workshops = CollectWorkshops(sheet, periodConfig, attendees);
                    allWorkshops.AddRange(workshops);
                    _logger.LogDebug("Found {Count} workshops in {SheetName}", workshops.Count, sheet.Name);
                }

                _logger.LogInformation("Total workshops parsed: {Count}", allWorkshops.Count);
                return allWorkshops;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse workshops from Excel package");
                throw new InvalidOperationException("Failed to parse workshops. Please verify the Excel file structure matches the expected schema.", ex);
            }
        }

        /// <summary>
        /// Loads all attendees from the ClassSelection sheet into a dictionary keyed by registration ID.
        /// Generates fallback IDs (FirstName+LastName) when ClassSelectionId is missing to ensure all attendees can be referenced.
        /// </summary>
        /// <param name="package">EPPlus ExcelPackage containing the ClassSelection sheet.</param>
        /// <param name="schema">Event schema defining ClassSelection sheet column mappings.</param>
        /// <returns>Dictionary mapping ClassSelectionId (or fallback ID) to Attendee objects.</returns>
        private Dictionary<string, Attendee> LoadAttendees(ExcelPackage package, EventSchema schema)
        {
            var attendees = new Dictionary<string, Attendee>();

            try
            {
                var sheetConfig = schema.ClassSelectionSheet;
                var sheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetConfig.SheetName);

                if (sheet == null)
                {
                    var availableSheets = string.Join(", ", package.Workbook.Worksheets.Select(ws => ws.Name));
                    _logger.LogWarning("ClassSelection sheet '{SheetName}' not found. Available sheets: {AvailableSheets}",
                        sheetConfig.SheetName, availableSheets);
                    throw new InvalidDataException($"Required sheet '{sheetConfig.SheetName}' not found in Excel file. Available sheets: {availableSheets}");
                }

                if (sheet.Dimension == null)
                {
                    _logger.LogWarning("ClassSelection sheet '{SheetName}' is empty", sheetConfig.SheetName);
                    return attendees;
                }

                var helper = new SheetHelper(sheet);
                var rows = sheet.Dimension.Rows;

                for (int row = 2; row <= rows; row++)
                {
                    try
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
                            _logger.LogDebug("Generated fallback ID for attendee: {FullName} -> {SelectionId}",
                                $"{firstName} {lastName}", selectionId);
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
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse attendee data at row {Row} in {SheetName}", row, sheetConfig.SheetName);
                        // Continue processing other rows
                    }
                }

                return attendees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load attendees from ClassSelection sheet");
                throw new InvalidOperationException("Failed to load attendees. Please verify the ClassSelection sheet structure.", ex);
            }
        }

        /// <summary>
        /// Collects workshops from a single period sheet by processing workshop columns and participant rows.
        /// Creates Workshop objects aggregated by unique workshop name, leader, period, and duration combination.
        /// </summary>
        /// <param name="sheet">Excel worksheet for a specific period (e.g., "MorningFirstPeriod").</param>
        /// <param name="periodConfig">Schema configuration defining period sheet structure and workshop columns.</param>
        /// <param name="attendees">Dictionary of attendees to link workshop selections to participant information.</param>
        /// <returns>List of Workshop objects with associated WorkshopSelection records.</returns>
        private List<Workshop> CollectWorkshops(ExcelWorksheet sheet, PeriodSheetConfig periodConfig, Dictionary<string, Attendee> attendees)
        {
            if (sheet.Dimension == null)
            {
                _logger.LogDebug("Sheet {SheetName} has no dimension (empty sheet)", sheet.Name);
                return new List<Workshop>();
            }

            try
            {
                var helper = new SheetHelper(sheet);
                var period = new Period(periodConfig.SheetName);
                period.DisplayName = periodConfig.DisplayName;
                var workshops = new Dictionary<string, Workshop>();
                var rows = sheet.Dimension.Rows;

                for (int row = 2; row <= rows; row++)
                {
                    try
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
                            try
                            {
                                var cellValue = helper.GetCellValue(row, workshopCol.ColumnName);
                                if (string.IsNullOrWhiteSpace(cellValue)) continue;

                                var workshopName = cellValue.GetWorkshopName();
                                var leaderName = cellValue.GetLeaderName();

                                if (string.IsNullOrWhiteSpace(workshopName))
                                {
                                    _logger.LogDebug("Skipping empty workshop name at row {Row}, column {Column}", row, workshopCol.ColumnName);
                                    continue;
                                }

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
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse workshop data at row {Row}, column {Column} in sheet {SheetName}",
                                    row, workshopCol.ColumnName, sheet.Name);
                                // Continue processing other workshop columns
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process row {Row} in sheet {SheetName}", row, sheet.Name);
                        // Continue processing other rows
                    }
                }

                return workshops.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect workshops from sheet {SheetName}", sheet.Name);
                throw new InvalidOperationException($"Failed to collect workshops from sheet '{sheet.Name}'. Please verify the sheet structure.", ex);
            }
        }

        /// <summary>
        /// Creates a comprehensive PDF document containing workshop rosters and individual participant schedules.
        /// Optionally includes blank schedules for walk-in participants.
        /// </summary>
        /// <param name="mergeWorkshopCells">Whether to merge cells for multi-day workshops in individual schedules.</param>
        /// <param name="timeslots">Custom timeslots for schedule structure. If null, uses default timeslots from schema.</param>
        /// <param name="blankScheduleCount">Number of blank schedule pages to append for walk-in participants.</param>
        /// <param name="eventName">Name of the event displayed in PDF headers and footers.</param>
        /// <returns>MigraDoc Document ready for rendering, or null if Workshops collection is empty.</returns>
        public Document? CreatePdf(bool mergeWorkshopCells = true, List<Models.TimeSlot>? timeslots = null, int blankScheduleCount = 0, string eventName = "Winter Adventure")
        {
            if (Workshops != null)
            {
                var document = new Document();

                foreach(var section in PrintWorkshopParticipants(eventName, timeslots))
                {
                    SetStandardMargins(section);
                    document.Sections.Add(section);
                }

                // Add individual schedules
                foreach(var section in PrintIndividualSchedules(eventName, mergeWorkshopCells, timeslots))
                {
                    SetStandardMargins(section);
                    document.Sections.Add(section);
                }

                // Add blank schedules
                if (blankScheduleCount > 0)
                {
                    _logger.LogInformation($"Generating {blankScheduleCount} blank schedules");
                    var blankSections = PrintBlankSchedules(eventName, blankScheduleCount, mergeWorkshopCells, timeslots);
                    _logger.LogInformation($"Generated {blankSections.Count} blank schedule sections");

                    foreach(var section in blankSections)
                    {
                        SetStandardMargins(section);
                        document.Sections.Add(section);
                    }
                }

                return document;
            }

            return null;
        }

        /// <summary>
        /// Creates a master schedule PDF showing all workshops organized by location, time, and days.
        /// Useful for event staff to see the complete workshop schedule at a glance.
        /// </summary>
        /// <param name="eventName">Name of the event displayed in PDF title.</param>
        /// <param name="timeslots">Custom timeslots for schedule structure. If null, uses default timeslots from schema.</param>
        /// <returns>MigraDoc Document ready for rendering, or null if Workshops collection is empty.</returns>
        public Document? CreateMasterSchedulePdf(string eventName = "Master Schedule", List<Models.TimeSlot>? timeslots = null)
        {
            if (Workshops != null)
            {
                var document = new Document();

                foreach(var section in PrintMasterSchedule(eventName, timeslots))
                {
                    SetStandardMargins(section);
                    document.Sections.Add(section);
                }

                return document;
            }

            return null;
        }

        /// <summary>
        /// Generates PDF sections for workshop rosters, one section per workshop.
        /// Each roster lists enrolled participants (choice #1) and backup/alternate choices (choice #2+) for workshop leaders.
        /// </summary>
        /// <param name="eventName">Name of the event displayed in section footers.</param>
        /// <param name="timeslots">Timeslots to find period time ranges. If null, time ranges are omitted from rosters.</param>
        /// <returns>List of MigraDoc Section objects, one per workshop.</returns>
        private List<Section> PrintWorkshopParticipants(string eventName, List<Models.TimeSlot>? timeslots = null)
        {
            var sections = new List<Section>();

            foreach(var workshopListing in Workshops)
            {
                var section = new Section();

                // Add logo to section
                AddLogoToSection(section, "roster");

                // Workshop name - Oswald (top level header)
                var header = section.AddParagraph();
                header.Format.Font.Name = FontNames.Oswald;
                header.Format.Font.Color = COLOR_BLACK;
                header.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.WorkshopTitle;
                header.Format.Alignment = ParagraphAlignment.Center;
                header.AddFormattedText(workshopListing.Name, TextFormat.Bold);

                // Leader info - Noto Sans (second level header)
                var leaderInfo = section.AddParagraph();
                leaderInfo.Format.Font.Name = FontNames.NotoSans;
                leaderInfo.Format.Font.Color = COLOR_BLACK;
                leaderInfo.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.LeaderInfo;
                leaderInfo.Format.Alignment = ParagraphAlignment.Center;
                leaderInfo.AddFormattedText(workshopListing.Leader);

                // Location info - Noto Sans
                if (!string.IsNullOrWhiteSpace(workshopListing.Location))
                {
                    var locationInfo = section.AddParagraph();
                    locationInfo.Format.Font.Name = FontNames.NotoSans;
                    locationInfo.Format.Font.Color = COLOR_BLACK;
                    locationInfo.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.LocationInfo;
                    locationInfo.Format.Alignment = ParagraphAlignment.Center;
                    locationInfo.AddFormattedText($"Location: {workshopListing.Location}");
                }

                // Period and duration info - Noto Sans (second level header)
                var periodInfo = section.AddParagraph();
                periodInfo.Format.Font.Name = FontNames.NotoSans;
                periodInfo.Format.Font.Color = COLOR_BLACK;
                periodInfo.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.PeriodInfo;
                periodInfo.Format.Alignment = ParagraphAlignment.Center;

                // Find the timeslot for this period to get the time range
                var periodTimeslot = timeslots?.FirstOrDefault(t =>
                    t.IsPeriod && t.Label == workshopListing.Period.DisplayName);

                string periodText = workshopListing.Period.DisplayName;
                if (periodTimeslot != null && !string.IsNullOrEmpty(periodTimeslot.TimeRange))
                {
                    periodText += $" ({periodTimeslot.TimeRange})";
                }
                periodText += $" - {workshopListing.Duration.Description}";

                periodInfo.AddFormattedText(periodText);
                periodInfo.Format.SpaceAfter = PdfLayoutConstants.Spacing.SectionSpacing;

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
                    firstChoiceHeader.Format.Font.Name = FontNames.NotoSans;
                    firstChoiceHeader.Format.Font.Color = COLOR_BLACK;
                    firstChoiceHeader.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.SectionHeader;
                    firstChoiceHeader.AddFormattedText($"Enrolled Participants ({firstChoiceAttendees.Count}):", TextFormat.Bold);
                    firstChoiceHeader.Format.SpaceAfter = Unit.FromPoint(8);

                    AddTwoColumnParticipantList(section, firstChoiceAttendees, showChoiceNumber: false);
                }

                // Backup participants
                if (backupAttendees.Any())
                {
                    var backupHeader = section.AddParagraph();
                    backupHeader.Format.Font.Name = FontNames.NotoSans;
                    backupHeader.Format.Font.Color = COLOR_BLACK;
                    backupHeader.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.SectionHeader;
                    backupHeader.Format.SpaceAfter = Unit.FromPoint(8);
                    backupHeader.Format.SpaceBefore = PdfLayoutConstants.Spacing.SectionSpacing;
                    backupHeader.AddFormattedText($"Backup/Alternate Choices ({backupAttendees.Count}):", TextFormat.Bold);

                    var backupNote = section.AddParagraph();
                    backupNote.Format.Font.Name = FontNames.Roboto;
                    backupNote.Format.Font.Color = COLOR_BLACK;
                    backupNote.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.BackupNote;
                    backupNote.Format.Font.Italic = true;
                    backupNote.Format.LeftIndent = PdfLayoutConstants.Spacing.BackupNoteIndent;
                    backupNote.Format.SpaceAfter = Unit.FromPoint(8);
                    backupNote.AddText("These participants may join if their first choice is full:");

                    AddTwoColumnParticipantList(section, backupAttendees, showChoiceNumber: true);
                }

                // Add event name footer
                AddEventNameFooter(section, eventName);

                sections.Add(section);
            }

            return sections;
        }

        /// <summary>
        /// Calculates adaptive font size for participant names to prevent text wrapping in roster columns.
        /// Uses progressively smaller fonts for longer names based on total text length including number and checkbox.
        /// </summary>
        /// <param name="fullName">Participant's full name.</param>
        /// <param name="counter">Numbered position in the list.</param>
        /// <param name="showChoiceNumber">Whether to include choice number in length calculation.</param>
        /// <param name="choiceNumber">Choice number (1=first choice, 2+=backup) if applicable.</param>
        /// <returns>Font size in points (10, 11, 13, or 15) based on calculated text length.</returns>
        private int GetParticipantFontSize(string fullName, int counter, bool showChoiceNumber, int? choiceNumber = null)
        {
            // Calculate full text length including number, name, choice indicator, and checkbox
            string text = $"{counter}. {fullName}";
            if (showChoiceNumber && choiceNumber.HasValue)
            {
                text += $" (Choice #{choiceNumber})";
            }
            text += " [\u2003]";  // Using em space for wider checkbox

            // Use progressively smaller fonts for longer names to prevent wrapping
            if (text.Length > PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.VeryLong)
                return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.VeryLong;
            if (text.Length > PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long)
                return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Long;
            if (text.Length > PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Medium)
                return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Medium;
            return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Default;
        }

        /// <summary>
        /// Sets standard margins on a section (0.5 inch on all sides)
        /// </summary>
        /// <param name="section">The section to configure</param>
        private void SetStandardMargins(Section section)
        {
            section.PageSetup.TopMargin = PdfLayoutConstants.Margins.Standard;
            section.PageSetup.LeftMargin = PdfLayoutConstants.Margins.Standard;
            section.PageSetup.RightMargin = PdfLayoutConstants.Margins.Standard;
            section.PageSetup.BottomMargin = PdfLayoutConstants.Margins.Standard;
        }

        /// <summary>
        /// Adds a two-column participant list table to maximize roster space efficiency.
        /// Applies adaptive font sizing and includes optional choice numbers for backup participants.
        /// </summary>
        /// <param name="section">MigraDoc section to add the table to.</param>
        /// <param name="participants">Workshop selections to display in the list.</param>
        /// <param name="showChoiceNumber">Whether to display choice numbers (e.g., "Choice #2") after participant names.</param>
        private void AddTwoColumnParticipantList(Section section, List<WorkshopSelection> participants, bool showChoiceNumber)
        {
            // Create a table with 2 columns
            var table = section.AddTable();
            table.Borders.Visible = false;

            // Define column widths (equal width)
            var columnWidth = PdfLayoutConstants.ColumnWidths.ParticipantColumn;
            table.AddColumn(columnWidth);
            table.AddColumn(columnWidth);

            // Split participants into two columns
            int halfCount = (int)Math.Ceiling(participants.Count / (double)PdfLayoutConstants.Table.TwoColumnCount);
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
                leftPara.Format.Font.Name = FontNames.Roboto;
                leftPara.Format.Font.Color = COLOR_BLACK;

                int leftCounter = i + 1;
                int leftFontSize = GetParticipantFontSize(
                    leftColumn[i].FullName,
                    leftCounter,
                    showChoiceNumber,
                    leftColumn[i].ChoiceNumber);

                leftPara.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.ParticipantName;
                leftPara.Format.LeftIndent = Unit.FromPoint(6);
                leftPara.Format.SpaceBefore = 0;
                leftPara.Format.SpaceAfter = Unit.FromPoint(6);
                leftPara.Format.LineSpacingRule = LineSpacingRule.Multiple;
                leftPara.Format.LineSpacing = PdfLayoutConstants.Table.ParticipantListLineSpacing;

                // Add number at base size (bold)
                leftPara.AddFormattedText($"{leftCounter}. ", TextFormat.Bold);

                // Add name at calculated size
                var nameText = leftPara.AddFormattedText(leftColumn[i].FullName);
                nameText.Font.Size = leftFontSize;

                if (showChoiceNumber)
                {
                    var choiceText = leftPara.AddFormattedText($" (Choice #{leftColumn[i].ChoiceNumber})");
                    choiceText.Font.Size = leftFontSize;
                }

                var checkboxText = leftPara.AddFormattedText(" [\u2003]");
                checkboxText.Font.Size = leftFontSize;

                // Right column participant (if exists)
                if (i < rightColumn.Count)
                {
                    var rightCell = row.Cells[1];
                    rightCell.VerticalAlignment = VerticalAlignment.Top;
                    var rightPara = rightCell.AddParagraph();
                    rightPara.Format.Font.Name = FontNames.Roboto;
                    rightPara.Format.Font.Color = COLOR_BLACK;

                    int rightCounter = halfCount + i + 1;
                    int rightFontSize = GetParticipantFontSize(
                        rightColumn[i].FullName,
                        rightCounter,
                        showChoiceNumber,
                        rightColumn[i].ChoiceNumber);

                    rightPara.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.ParticipantName;
                    rightPara.Format.LeftIndent = Unit.FromPoint(6);
                    rightPara.Format.SpaceBefore = 0;
                    rightPara.Format.SpaceAfter = Unit.FromPoint(6);
                    rightPara.Format.LineSpacingRule = LineSpacingRule.Multiple;
                    rightPara.Format.LineSpacing = PdfLayoutConstants.Table.ParticipantListLineSpacing;

                    // Add number at base size (bold)
                    rightPara.AddFormattedText($"{rightCounter}. ", TextFormat.Bold);

                    // Add name at calculated size
                    var rightNameText = rightPara.AddFormattedText(rightColumn[i].FullName);
                    rightNameText.Font.Size = rightFontSize;

                    if (showChoiceNumber)
                    {
                        var rightChoiceText = rightPara.AddFormattedText($" (Choice #{rightColumn[i].ChoiceNumber})");
                        rightChoiceText.Font.Size = rightFontSize;
                    }

                    var rightCheckboxText = rightPara.AddFormattedText(" [\u2003]");
                    rightCheckboxText.Font.Size = rightFontSize;
                }
            }
        }

        /// <summary>
        /// Generates landscape-oriented individual schedule pages for each registered participant.
        /// Shows participant's workshops across all days and periods in a visual calendar format.
        /// </summary>
        /// <param name="eventName">Name of the event displayed in section footers.</param>
        /// <param name="mergeWorkshopCells">Whether to merge table cells for multi-day workshops to show continuity.</param>
        /// <param name="timeslots">Timeslots defining schedule structure. If null, uses default timeslots from schema.</param>
        /// <returns>List of MigraDoc Section objects, one per participant.</returns>
        private List<Section> PrintIndividualSchedules(string eventName, bool mergeWorkshopCells = true, List<Models.TimeSlot>? timeslots = null)
        {
            var sections = new List<Section>();

            if (_schema == null)
            {
                _logger.LogWarning("Cannot generate individual schedules - schema not loaded");
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
                SetStandardMargins(section);

                // Add logo to section
                AddLogoToSection(section, "individual");

                // Header with attendee name - Oswald
                var header = section.AddParagraph();
                header.Format.Font.Name = FontNames.Oswald;
                header.Format.Font.Color = COLOR_BLACK;
                header.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.ParticipantName;
                header.Format.Alignment = ParagraphAlignment.Center;
                header.AddFormattedText($"{attendee.FullName}'s Schedule", TextFormat.Bold);
                header.Format.SpaceAfter = PdfLayoutConstants.Spacing.HeaderSpacing;

                // Create schedule table
                var table = section.AddTable();
                table.Borders.Width = PdfLayoutConstants.Table.BorderWidth;

                // Column widths: 11" page - 1" margins = 10" usable width
                // Make table narrower (9.2") and center it with left indent
                var timeColumnWidth = PdfLayoutConstants.ColumnWidths.IndividualSchedule.Time;
                var dayColumnWidth = PdfLayoutConstants.ColumnWidths.IndividualSchedule.Day;
                var tableWidth = timeColumnWidth + (_schema.TotalDays * dayColumnWidth); // 1.8 + 7.4 = 9.2

                table.Rows.LeftIndent = Unit.FromInch(PdfLayoutConstants.Table.IndividualScheduleLeftIndent);

                table.AddColumn(Unit.FromInch(PdfLayoutConstants.ColumnWidths.IndividualSchedule.Time));

                // Columns 1-4: Days
                for (int i = 0; i < _schema.TotalDays; i++)
                {
                    table.AddColumn(Unit.FromInch(PdfLayoutConstants.ColumnWidths.IndividualSchedule.Day));
                }

                // Header row with day numbers
                var headerRow = table.AddRow();
                headerRow.Shading.Color = Color.FromRgb(220, 220, 220);
                headerRow.HeadingFormat = true;

                var periodHeaderCell = headerRow.Cells[0];
                var periodHeaderPara = periodHeaderCell.AddParagraph();
                periodHeaderPara.Format.Font.Name = FontNames.NotoSans;
                periodHeaderPara.Format.Font.Bold = true;
                periodHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.PeriodHeader;
                periodHeaderPara.Format.Alignment = ParagraphAlignment.Center;
                periodHeaderPara.AddText("Time");

                for (int day = 1; day <= _schema.TotalDays; day++)
                {
                    var dayCell = headerRow.Cells[day];
                    var dayPara = dayCell.AddParagraph();
                    dayPara.Format.Font.Name = FontNames.NotoSans;
                    dayPara.Format.Font.Bold = true;
                    dayPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.DayIndicator;
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
                            timePara.Format.Font.Name = FontNames.Roboto;
                            timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.TimeSlot;
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

                                // Add workshop content with adaptive font sizing
                                var workshopPara = dayCell.AddParagraph();
                                workshopPara.Format.Alignment = ParagraphAlignment.Center;

                                // Calculate adaptive font size based on workshop name length
                                var workshopDisplayText = isLeading ? $"Leading {workshop.Name}" : workshop.Name;
                                int workshopFontSize;
                                if (workshopDisplayText.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.VeryLong)
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.VeryLong;
                                }
                                else if (workshopDisplayText.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long)
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Long;
                                }
                                else if (workshopDisplayText.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Medium)
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Medium;
                                }
                                else
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Default;
                                }

                                workshopPara.Format.Font.Size = workshopFontSize;

                                if (isLeading)
                                {
                                    workshopPara.Format.Font.Name = FontNames.NotoSans;
                                    workshopPara.Format.Font.Bold = true;
                                    workshopPara.AddText(workshopDisplayText);
                                }
                                else
                                {
                                    workshopPara.Format.Font.Name = FontNames.Roboto;
                                    workshopPara.AddText(workshopDisplayText);
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
                                            leaderPara.Format.Font.Name = FontNames.Roboto;
                                            leaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.LeaderName;
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
                                    leaderPara.Format.Font.Name = FontNames.Roboto;
                                    leaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.LeaderName;
                                    leaderPara.Format.Font.Italic = true;
                                    leaderPara.Format.Alignment = ParagraphAlignment.Center;
                                    leaderPara.AddText($"{workshop.Leader}");
                                }

                                // Add location info with tags
                                if (!string.IsNullOrWhiteSpace(workshop.Location))
                                {
                                    var locationPara = dayCell.AddParagraph();
                                    locationPara.Format.Font.Name = FontNames.Roboto;
                                    locationPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.LocationName;
                                    locationPara.Format.Font.Bold = true;
                                    locationPara.Format.Alignment = ParagraphAlignment.Center;

                                    // Add location name in bold
                                    locationPara.AddText(workshop.Location);

                                    // Add tags in lowercase and not bold
                                    if (workshop.Tags != null && workshop.Tags.Any())
                                    {
                                        var tagNames = string.Join(", ", workshop.Tags
                                            .OrderBy(t => t.Name)
                                            .Select(t => t.Name.ToLower()));
                                        var tagText = locationPara.AddFormattedText($" ({tagNames})");
                                        tagText.Font.Bold = false;
                                    }
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

                // Add facility map to the footer
                AddFacilityMapToSection(section);

                // Add event name footer
                AddEventNameFooter(section, eventName);

                sections.Add(section);
            }

            return sections;
        }

        /// <summary>
        /// Generates blank schedule templates for walk-in participants who register on-site.
        /// Each schedule includes a name field and empty master schedule grid to be filled in manually.
        /// </summary>
        /// <param name="eventName">Name of the event displayed in section footers.</param>
        /// <param name="count">Number of blank schedule copies to generate.</param>
        /// <param name="mergeWorkshopCells">Whether to merge cells for multi-day workshops (currently unused for blank schedules).</param>
        /// <param name="timeslots">Timeslots defining schedule structure. If null, uses default timeslots from schema.</param>
        /// <returns>List of MigraDoc Section objects containing blank schedules.</returns>
        private List<Section> PrintBlankSchedules(string eventName, int count, bool mergeWorkshopCells = true, List<Models.TimeSlot>? timeslots = null)
        {
            _logger.LogInformation($"PrintBlankSchedules called with count={count}");
            var sections = new List<Section>();

            // Generate multiple copies of the master schedule for blank schedules
            for (int i = 0; i < count; i++)
            {
                // Get master schedule sections (typically just one section)
                var masterSections = PrintMasterSchedule(eventName: "Workshop Schedule", timeslots: timeslots);

                foreach (var section in masterSections)
                {
                    // Insert a name field at the very top of the section
                    var nameField = new Paragraph();
                    nameField.Format.Font.Name = FontNames.NotoSans;
                    nameField.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.NameFieldLabel;
                    nameField.Format.Alignment = ParagraphAlignment.Left;
                    nameField.Format.SpaceAfter = Unit.FromPoint(8);
                    nameField.AddText("Name: _____________________________");

                    // Insert at the beginning of the section
                    section.Elements.InsertObject(0, nameField);

                    // Add facility map to the footer
                    AddFacilityMapToSection(section);

                    // Add event name footer
                    AddEventNameFooter(section, eventName);

                    sections.Add(section);
                }
            }

            _logger.LogInformation($"Generated {sections.Count} blank schedule sections");
            return sections;
        }

        /// <summary>
        /// Adds a merged row to schedule table for non-period activities that span all days.
        /// Used for activities like Breakfast, Lunch, or Evening Program that don't vary by day.
        /// </summary>
        /// <param name="table">MigraDoc table to add the row to.</param>
        /// <param name="activityName">Name of the activity (e.g., "Breakfast", "Lunch").</param>
        /// <param name="totalDays">Number of days to merge across (typically 4 for WinterAdventure).</param>
        /// <param name="timeRange">Optional time range to display (e.g., "8:00 AM - 9:00 AM"). Empty if time is not fixed.</param>
        private void AddMergedActivityRow(Table table, string activityName, int totalDays, string timeRange = "")
        {
            var row = table.AddRow();
            row.Shading.Color = Color.FromRgb(230, 230, 230);

            // First cell: time range (not merged)
            var timeCell = row.Cells[0];
            if (!string.IsNullOrWhiteSpace(timeRange))
            {
                var timePara = timeCell.AddParagraph();
                timePara.Format.Font.Name = FontNames.Roboto;
                timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.TimeSlot;
                timePara.Format.Alignment = ParagraphAlignment.Center;
                timePara.AddText(timeRange);
            }

            // Second cell: activity name merged across all day columns
            var activityCell = row.Cells[1];
            activityCell.MergeRight = totalDays - 1; // Merge across day columns only

            var para = activityCell.AddParagraph();
            para.Format.Font.Name = FontNames.Roboto;
            para.Format.Font.Size = PdfLayoutConstants.FontSizes.BlankSchedule.Title;
            para.Format.Font.Italic = true;
            para.Format.Alignment = ParagraphAlignment.Center;
            para.AddFormattedText(activityName, TextFormat.Bold);
        }

        /// <summary>
        /// Creates default timeslots for schedule structure when custom timeslots are not provided.
        /// Includes Breakfast, all period sheets from schema, Lunch, and Evening Program.
        /// </summary>
        /// <returns>List of TimeSlot objects representing the event's daily schedule structure.</returns>
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

        /// <summary>
        /// Generates master schedule grid showing all workshops organized by location, time, and days.
        /// Automatically selects landscape or portrait orientation based on number of locations.
        /// </summary>
        /// <param name="eventName">Name of the event displayed in PDF title.</param>
        /// <param name="timeslots">Timeslots defining schedule structure. If null, uses default timeslots from schema.</param>
        /// <returns>List containing a single MigraDoc Section with the master schedule table.</returns>
        private List<Section> PrintMasterSchedule(string eventName, List<Models.TimeSlot>? timeslots = null)
        {
            var sections = new List<Section>();

            // If no timeslots provided, create default set
            if (timeslots == null || !timeslots.Any())
            {
                timeslots = CreateDefaultTimeslots();
            }

            // Get unique locations sorted alphabetically
            var locations = GetUniqueLocations();

            if (!locations.Any())
            {
                // No locations assigned, skip master schedule
                return sections;
            }

            var section = new Section();

            // Auto-detect orientation based on location count
            if (locations.Count > 5)
            {
                section.PageSetup.Orientation = Orientation.Landscape;
                // Smaller margins to maximize space
                section.PageSetup.LeftMargin = Unit.FromInch(0.4);
                section.PageSetup.RightMargin = Unit.FromInch(0.4);
                section.PageSetup.TopMargin = Unit.FromInch(0.4);
                section.PageSetup.BottomMargin = Unit.FromInch(0.4);
            }
            else
            {
                section.PageSetup.Orientation = Orientation.Portrait;
                SetStandardMargins(section);
            }

            // Add logo to section
            AddLogoToSection(section, "master");

            // Title
            var title = section.AddParagraph();
            title.Format.Font.Name = FontNames.Oswald;
            title.Format.Font.Color = COLOR_BLACK;
            title.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.Title;
            title.Format.Alignment = ParagraphAlignment.Center;
            title.AddFormattedText(eventName, TextFormat.Bold);
            // Add vertical spacing to center content on page
            title.Format.SpaceBefore = section.PageSetup.Orientation == Orientation.Landscape
                ? Unit.FromPoint(24)
                : Unit.FromPoint(12);
            title.Format.SpaceAfter = Unit.FromPoint(12);

            // Create table
            var table = section.AddTable();
            table.Borders.Width = PdfLayoutConstants.Table.BorderWidth;

            // Column widths - make table narrower than full width to allow centering
            var timeColumnWidth = PdfLayoutConstants.ColumnWidths.MasterSchedule.Time;
            var daysColumnWidth = PdfLayoutConstants.ColumnWidths.MasterSchedule.Days;
            var pageUsableWidth = section.PageSetup.Orientation == Orientation.Landscape
                ? 10.2  // 11" - 0.4" - 0.4"
                : PdfLayoutConstants.PageDimensions.PortraitUsableWidth;
            // Make table 9.5" wide to leave room for centering
            var tableWidth = section.PageSetup.Orientation == Orientation.Landscape ? 9.5 : pageUsableWidth;
            var locationColumnWidth = (tableWidth - timeColumnWidth - daysColumnWidth) / locations.Count;

            // Add left indent to center the table horizontally (slightly more to the right)
            if (section.PageSetup.Orientation == Orientation.Landscape)
            {
                var leftIndent = (pageUsableWidth - tableWidth) / 2 + 0.15;  // Center + extra shift right
                table.Rows.LeftIndent = Unit.FromInch(leftIndent);
            }

            // Add columns
            table.AddColumn(Unit.FromInch(timeColumnWidth));  // Time
            table.AddColumn(Unit.FromInch(daysColumnWidth));  // Days indicator
            foreach (var location in locations)
            {
                table.AddColumn(Unit.FromInch(locationColumnWidth));
            }

            // Set minimal cell padding for the entire table
            foreach (Column column in table.Columns)
            {
                column.LeftPadding = Unit.FromPoint(2);
                column.RightPadding = Unit.FromPoint(2);
            }

            // Header row
            var headerRow = table.AddRow();
            headerRow.Shading.Color = Color.FromRgb(220, 220, 220);
            headerRow.HeadingFormat = true;
            headerRow.TopPadding = Unit.FromPoint(3);
            headerRow.BottomPadding = Unit.FromPoint(3);

            // Time header
            var timeHeader = headerRow.Cells[0];
            var timeHeaderPara = timeHeader.AddParagraph();
            timeHeaderPara.Format.Font.Name = FontNames.NotoSans;
            timeHeaderPara.Format.Font.Bold = true;
            timeHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.ColumnHeader;
            timeHeaderPara.Format.Alignment = ParagraphAlignment.Center;
            timeHeaderPara.AddText("Time");

            // Days header
            var daysHeader = headerRow.Cells[1];
            var daysHeaderPara = daysHeader.AddParagraph();
            daysHeaderPara.Format.Font.Name = FontNames.NotoSans;
            daysHeaderPara.Format.Font.Bold = true;
            daysHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.ColumnHeader;
            daysHeaderPara.Format.Alignment = ParagraphAlignment.Center;
            daysHeaderPara.AddText("Days");

            // Location headers
            for (int i = 0; i < locations.Count; i++)
            {
                var locationHeader = headerRow.Cells[i + 2];
                var locationHeaderPara = locationHeader.AddParagraph();
                locationHeaderPara.Format.Font.Name = FontNames.NotoSans;
                locationHeaderPara.Format.Font.Bold = true;
                locationHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.LocationHeader;
                locationHeaderPara.Format.Alignment = ParagraphAlignment.Center;
                locationHeaderPara.AddText(locations[i]);
            }

            // Add rows for each timeslot
            foreach (var timeslot in timeslots)
            {
                if (timeslot.IsPeriod)
                {
                    // Period: create two sub-rows for Days 1-2 and Days 3-4
                    AddPeriodRows(table, timeslot, locations);
                }
                else
                {
                    // Activity: create single merged row
                    AddActivityRow(table, timeslot, locations.Count);
                }
            }

            sections.Add(section);
            return sections;
        }

        /// <summary>
        /// Adds period workshop rows to master schedule table, split by day ranges (Days 1-2 and Days 3-4).
        /// Each row shows workshops assigned to each location for that time period and day range.
        /// </summary>
        /// <param name="table">MigraDoc table to add rows to.</param>
        /// <param name="timeslot">Period timeslot containing workshop assignments.</param>
        /// <param name="locations">Ordered list of locations defining table columns.</param>
        private void AddPeriodRows(Table table, Models.TimeSlot timeslot, List<string> locations)
        {
            // Create two rows: Days 1-2 and Days 3-4
            var row12 = table.AddRow();
            row12.TopPadding = Unit.FromPoint(2);
            row12.BottomPadding = Unit.FromPoint(2);

            var row34 = table.AddRow();
            row34.TopPadding = Unit.FromPoint(2);
            row34.BottomPadding = Unit.FromPoint(2);

            // Time cell (merge across both rows)
            var timeCell = row12.Cells[0];
            timeCell.MergeDown = 1;
            timeCell.VerticalAlignment = VerticalAlignment.Center;
            var timePara = timeCell.AddParagraph();
            timePara.Format.Font.Name = FontNames.NotoSans;
            timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.TimeCell;
            timePara.Format.Alignment = ParagraphAlignment.Center;

            if (!string.IsNullOrEmpty(timeslot.TimeRange))
            {
                timePara.AddText(timeslot.TimeRange);
            }
            else
            {
                timePara.AddText(timeslot.Label);
            }

            // Days 1-2 cell
            var days12Cell = row12.Cells[1];
            days12Cell.VerticalAlignment = VerticalAlignment.Center;
            var days12Para = days12Cell.AddParagraph();
            days12Para.Format.Font.Name = FontNames.NotoSans;
            days12Para.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.DayIndicator;
            days12Para.Format.Alignment = ParagraphAlignment.Center;
            days12Para.AddText("Days\n1-2");

            // Days 3-4 cell
            var days34Cell = row34.Cells[1];
            days34Cell.VerticalAlignment = VerticalAlignment.Center;
            var days34Para = days34Cell.AddParagraph();
            days34Para.Format.Font.Name = FontNames.NotoSans;
            days34Para.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.DayIndicator;
            days34Para.Format.Alignment = ParagraphAlignment.Center;
            days34Para.AddText("Days\n3-4");

            // Location columns
            for (int i = 0; i < locations.Count; i++)
            {
                var location = locations[i];

                // Check for 4-day workshop first
                var workshop4Day = Workshops.FirstOrDefault(w =>
                    w.Period.DisplayName == timeslot.Label &&
                    w.Location == location &&
                    w.Duration.StartDay == 1 &&
                    w.Duration.EndDay == 4);

                if (workshop4Day != null)
                {
                    // 4-day workshop spans both rows
                    var cell = row12.Cells[i + 2];
                    cell.MergeDown = 1;
                    cell.VerticalAlignment = VerticalAlignment.Center;
                    AddWorkshopToCell(cell, workshop4Day);
                }
                else
                {
                    // Check for Days 1-2 workshop
                    var workshop12 = Workshops.FirstOrDefault(w =>
                        w.Period.DisplayName == timeslot.Label &&
                        w.Location == location &&
                        w.Duration.StartDay == 1 &&
                        w.Duration.EndDay == 2);

                    if (workshop12 != null)
                    {
                        AddWorkshopToCell(row12.Cells[i + 2], workshop12);
                    }

                    // Check for Days 3-4 workshop
                    var workshop34 = Workshops.FirstOrDefault(w =>
                        w.Period.DisplayName == timeslot.Label &&
                        w.Location == location &&
                        w.Duration.StartDay == 3 &&
                        w.Duration.EndDay == 4);

                    if (workshop34 != null)
                    {
                        AddWorkshopToCell(row34.Cells[i + 2], workshop34);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a single merged row to master schedule for non-period activities.
        /// Used for activities like Breakfast or Lunch that span all locations uniformly.
        /// </summary>
        /// <param name="table">MigraDoc table to add the row to.</param>
        /// <param name="timeslot">Activity timeslot containing label and time information.</param>
        /// <param name="locationCount">Number of location columns to merge across.</param>
        private void AddActivityRow(Table table, Models.TimeSlot timeslot, int locationCount)
        {
            var row = table.AddRow();
            row.Shading.Color = Color.FromRgb(230, 230, 230);
            row.TopPadding = Unit.FromPoint(2);
            row.BottomPadding = Unit.FromPoint(2);

            // Time cell
            var timeCell = row.Cells[0];
            var timePara = timeCell.AddParagraph();
            timePara.Format.Font.Name = FontNames.NotoSans;
            timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.TimeCell;
            timePara.Format.Alignment = ParagraphAlignment.Center;

            if (!string.IsNullOrEmpty(timeslot.TimeRange))
            {
                timePara.AddText(timeslot.TimeRange);
            }
            else
            {
                timePara.AddText(timeslot.Label);
            }

            // Merge days and all location columns
            var activityCell = row.Cells[1];
            activityCell.MergeRight = locationCount; // Merge Days + all location columns
            activityCell.VerticalAlignment = VerticalAlignment.Center;
            var activityPara = activityCell.AddParagraph();
            activityPara.Format.Font.Name = FontNames.Roboto;
            activityPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.ActivityName;
            activityPara.Format.Font.Italic = true;
            activityPara.Format.Alignment = ParagraphAlignment.Center;
            activityPara.AddFormattedText(timeslot.Label, TextFormat.Bold);
        }

        /// <summary>
        /// Populates a master schedule table cell with workshop name and leader information.
        /// Formats the cell with appropriate font sizing and styling for schedule readability.
        /// </summary>
        /// <param name="cell">MigraDoc table cell to populate.</param>
        /// <param name="workshop">Workshop object containing name and leader information to display.</param>
        private void AddWorkshopToCell(Cell cell, Workshop workshop)
        {
            cell.VerticalAlignment = VerticalAlignment.Center;
            var para = cell.AddParagraph();
            para.Format.Font.Name = FontNames.NotoSans;
            para.Format.Alignment = ParagraphAlignment.Center;

            // Calculate adaptive font size based on workshop name length
            int workshopFontSize;
            if (workshop.Name.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.VeryLong)
            {
                workshopFontSize = 7;  // Very long names (45+ chars)
            }
            else if (workshop.Name.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long)
            {
                workshopFontSize = 8;  // Long names (35-44 chars)
            }
            else if (workshop.Name.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Medium)
            {
                workshopFontSize = 8;  // Medium names (28-34 chars)
            }
            else
            {
                workshopFontSize = PdfLayoutConstants.FontSizes.MasterSchedule.WorkshopInfo;  // Default: 9pt
            }

            para.Format.Font.Size = workshopFontSize;

            // Workshop name
            para.AddFormattedText(workshop.Name, TextFormat.Bold);
            para.AddLineBreak();

            // Leader in parentheses
            var leaderText = para.AddFormattedText($"({workshop.Leader})");
            leaderText.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.LeaderName;
            leaderText.Font.Italic = true;
        }

        /// <summary>
        /// Extracts unique workshop locations from Workshops collection for master schedule organization.
        /// Filters out empty locations and returns sorted list for consistent column ordering.
        /// </summary>
        /// <returns>Sorted list of unique, non-empty location strings.</returns>
        private List<string> GetUniqueLocations()
        {
            return Workshops
                .Where(w => !string.IsNullOrWhiteSpace(w.Location))
                .Select(w => w.Location)
                .Distinct()
                .OrderBy(loc => loc)
                .ToList();
        }

        /// <summary>
        /// Adds ECRS logo to PDF section with positioning and sizing based on document type.
        /// Different document types (roster, individual, master) have different logo placements for optimal layout.
        /// </summary>
        /// <param name="section">MigraDoc section to add logo to.</param>
        /// <param name="documentType">Type of document ("roster", "individual", or "master") determining logo position.</param>
        private void AddLogoToSection(Section section, string documentType = "roster")
        {
            try
            {
                // Load logo from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "WinterAdventurer.Library.Resources.Images.ECRS_Logo_Minimal_Gray.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Save to temporary file (MigraDoc requires file path for images)
                        var tempPath = Path.Combine(Path.GetTempPath(), "ecrs_logo_temp.png");
                        using (var fileStream = File.Create(tempPath))
                        {
                            stream.CopyTo(fileStream);
                        }

                        // Add logo with position/size based on document type
                        var logo = section.AddImage(tempPath);
                        logo.LockAspectRatio = true;
                        logo.RelativeVertical = RelativeVertical.Page;
                        logo.RelativeHorizontal = RelativeHorizontal.Margin;
                        logo.WrapFormat.Style = WrapStyle.Through;

                        // Adjust size and position based on document type
                        if (documentType == "individual")
                        {
                            // Individual schedules are landscape - logo on far right
                            logo.Height = PdfLayoutConstants.Logo.Height;
                            logo.Top = PdfLayoutConstants.Logo.MasterScheduleLandscape.Top;
                            logo.Left = PdfLayoutConstants.Logo.MasterScheduleLandscape.Left; // Far right for landscape
                        }
                        else if (documentType == "master")
                        {
                            // Master schedule - check orientation for proper logo placement
                            logo.Height = PdfLayoutConstants.Logo.Height;

                            if (section.PageSetup.Orientation == Orientation.Landscape)
                            {
                                logo.Top = PdfLayoutConstants.Logo.MasterScheduleLandscape.Top;
                                logo.Left = PdfLayoutConstants.Logo.MasterScheduleLandscape.Left;
                            }
                            else
                            {
                                logo.Top = PdfLayoutConstants.Logo.WorkshopRosterPortrait.Top;
                                logo.Left = PdfLayoutConstants.Logo.WorkshopRosterPortrait.Left;
                            }
                        }
                        else // roster (default)
                        {
                            // Class rosters - portrait, bottom right to avoid overlapping long workshop names
                            // Page is 11" tall with 0.5" margins = 10" content area
                            // Position at 10" - 1.0" logo - 0.2" margin = 8.8" from top
                            logo.Height = PdfLayoutConstants.Logo.Height;
                            logo.Top = PdfLayoutConstants.Logo.IndividualScheduleBottom.Top;
                            logo.Left = PdfLayoutConstants.Logo.IndividualScheduleBottom.Left;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail PDF generation
                _logger.LogWarning(ex, "Error adding logo to PDF section (type: {DocumentType})", documentType);
            }
        }

        /// <summary>
        /// Adds a footer to the section with the event name centered at the bottom
        /// </summary>
        private void AddEventNameFooter(Section section, string eventName)
        {
            var footer = section.Footers.Primary;
            var paragraph = footer.AddParagraph();
            paragraph.Format.Font.Name = FontNames.NotoSans;
            paragraph.Format.Font.Size = PdfLayoutConstants.FontSizes.EventFooter;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddText(eventName);
        }

        /// <summary>
        /// Adds Watson facility map image to PDF section to help participants navigate the venue.
        /// Map is centered and sized appropriately for the page layout.
        /// </summary>
        /// <param name="section">MigraDoc section to add the map to.</param>
        private void AddFacilityMapToSection(Section section)
        {
            try
            {
                // Load facility map from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "WinterAdventurer.Library.Resources.Images.watson_map.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Save to temporary file (MigraDoc requires file path for images)
                        var tempPath = Path.Combine(Path.GetTempPath(), "watson_map_temp.png");
                        using (var fileStream = File.Create(tempPath))
                        {
                            stream.CopyTo(fileStream);
                        }

                        // Add spacing before map
                        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(8);

                        // Add facility map centered
                        var mapParagraph = section.AddParagraph();
                        mapParagraph.Format.Alignment = ParagraphAlignment.Center;
                        var map = mapParagraph.AddImage(tempPath);
                        map.LockAspectRatio = true;
                        map.Width = PdfLayoutConstants.FacilityMap.Width; // Smaller map to fit on one page
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail PDF generation
                _logger.LogWarning(ex, "Error adding facility map to PDF section");
            }
        }
    }
}

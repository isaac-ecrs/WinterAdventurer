using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using WinterAdventurer.Library.Extensions;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.EventSchemas;
using WinterAdventurer.Library.Services;
using WinterAdventurer.Library.Exceptions;
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
    /// <summary>
    /// Main entry point for Excel import and PDF generation in the WinterAdventurer system.
    /// Provides comprehensive functionality for parsing workshop registration data from Excel files
    /// and generating class rosters, individual schedules, and master schedules as PDF documents.
    /// Uses a schema-driven architecture where Excel column mappings are defined in JSON configuration files.
    ///
    /// NOTE: This class now acts as a facade, delegating to specialized service classes while maintaining
    /// backward compatibility with existing code. For new code, consider using the service classes directly:
    /// - ExcelParser for Excel import
    /// - PdfDocumentOrchestrator for PDF generation
    /// </summary>
    public class ExcelUtilities
    {
        /// <summary>
        /// Collection of all workshops parsed from the Excel file.
        /// Each workshop is uniquely identified by the combination of Period, Name, Leader, and Duration.
        /// Populated by calling <see cref="ImportExcel"/> or <see cref="ParseWorkshops"/>.
        /// </summary>
        public List<Workshop> Workshops = new List<Workshop>();

        /// <summary>
        /// Event schema configuration loaded from embedded JSON resource.
        /// Defines Excel column mappings and workshop parsing rules for the event.
        /// </summary>
        private EventSchema? _schema;

        /// <summary>
        /// Logger instance for diagnostic information, warnings, and error reporting during Excel parsing and PDF generation.
        /// </summary>
        private readonly ILogger<ExcelUtilities> _logger;

        /// <summary>
        /// Excel parser service for importing workshop data from Excel files.
        /// </summary>
        private readonly ExcelParser _excelParser;

        /// <summary>
        /// PDF document orchestrator for coordinating PDF generation across multiple generators.
        /// </summary>
        private readonly PdfDocumentOrchestrator _pdfOrchestrator;

        /// <summary>
        /// Standard black color used throughout PDF document generation.
        /// </summary>
        readonly Color COLOR_BLACK = Color.FromRgb(0, 0, 0);

        public ExcelUtilities(ILogger<ExcelUtilities> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            GlobalFontSettings.FontResolver = new CustomFontResolver();

            // Load schema for service dependencies
            _schema = LoadEventSchema();

            // Create service dependencies
            var excelParserLogger = new LoggerFactory().CreateLogger<ExcelParser>();
            _excelParser = new ExcelParser(excelParserLogger);

            var rosterLogger = new LoggerFactory().CreateLogger<WorkshopRosterGenerator>();
            var rosterGenerator = new WorkshopRosterGenerator(rosterLogger);

            var scheduleLogger = new LoggerFactory().CreateLogger<IndividualScheduleGenerator>();
            var masterScheduleLogger = new LoggerFactory().CreateLogger<MasterScheduleGenerator>();
            var masterScheduleGenerator = new MasterScheduleGenerator(_schema, masterScheduleLogger);
            var scheduleGenerator = new IndividualScheduleGenerator(_schema, masterScheduleGenerator, scheduleLogger);

            var orchestratorLogger = new LoggerFactory().CreateLogger<PdfDocumentOrchestrator>();
            _pdfOrchestrator = new PdfDocumentOrchestrator(
                rosterGenerator,
                scheduleGenerator,
                masterScheduleGenerator,
                orchestratorLogger);
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
                // Delegate to ExcelParser service
                Workshops = _excelParser.ParseFromStream(stream);
                _logger.LogInformation("Excel import completed successfully via ExcelParser, {Count} workshops parsed", Workshops.Count);
            }
            catch (ExcelParsingException ex)
            {
                _logger.LogError(ex, "Failed to import Excel file");
                // Double-wrap to maintain backward compatibility with old exception structure
                // Old code: InvalidOperationException -> InvalidOperationException -> underlying exception
                var innerException = new InvalidOperationException(ex.Message, ex);
                throw new InvalidOperationException("Failed to import Excel file. Please verify the file format matches the expected schema.", innerException);
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
            // Delegate to ExcelParser service
            _excelParser.DumpExcelSchema(stream, outputPath);
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
            // Delegate to PdfDocumentOrchestrator service
            return _pdfOrchestrator.CreateWorkshopAndSchedulePdf(
                workshops: Workshops,
                eventName: eventName,
                mergeWorkshopCells: mergeWorkshopCells,
                timeslots: timeslots,
                blankScheduleCount: blankScheduleCount);
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
            // Delegate to PdfDocumentOrchestrator service
            return _pdfOrchestrator.CreateMasterSchedulePdf(
                workshops: Workshops,
                eventName: eventName,
                timeslots: timeslots);
        }
    }
}

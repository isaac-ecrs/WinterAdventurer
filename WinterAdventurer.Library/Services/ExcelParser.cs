using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml;
using WinterAdventurer.Library.EventSchemas;
using WinterAdventurer.Library.Exceptions;
using WinterAdventurer.Library.Extensions;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Parses Excel files containing workshop registration data using schema-driven configuration.
    /// Extracts attendees and workshop selections from structured Excel exports.
    /// </summary>
    public partial class ExcelParser
    {
        private readonly ILogger<ExcelParser> _logger;
        private EventSchema? _schema;

        /// <summary>
        /// Initializes a new instance of the ExcelParser class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public ExcelParser(ILogger<ExcelParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    throw new MissingResourceException($"Could not find embedded resource: {resourceName}")
                    {
                        ResourceName = resourceName,
                        Section = "SchemaLoading"
                    };
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var schema = JsonConvert.DeserializeObject<EventSchema>(json);
                    if (schema == null)
                    {
                        throw new SchemaValidationException("Failed to deserialize event schema")
                        {
                            SchemaName = resourceName
                        };
                    }
                    return schema;
                }
            }
        }

        /// <summary>
        /// Parses workshops from an Excel file stream.
        /// </summary>
        /// <param name="stream">Excel file stream to import. Stream will be reset to position 0.</param>
        /// <returns>List of parsed Workshop objects with associated participant selections.</returns>
        public List<Workshop> ParseFromStream(Stream stream)
        {
            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("WinterAdventurer");

                if (stream == null || stream.Length == 0)
                {
                    throw new ExcelParsingException("Excel file stream is empty or null");
                }

                stream.Position = 0;

                LogInformationStartingExcelImport(stream.Length);

                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        throw new ExcelParsingException("Excel file contains no worksheets");
                    }

                    LogDebugExcelPackageLoaded(package.Workbook.Worksheets.Count);
                    var workshops = ParseWorkshops(package);
                    LogInformationExcelImportCompleted(workshops.Count);
                    return workshops;
                }
            }
            catch (ExcelParsingException)
            {
                // Re-throw our custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                LogErrorFailedToImportExcel(ex);
                throw new ExcelParsingException("Failed to import Excel file. Please verify the file format matches the expected schema.", ex);
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
                LogInformationExcelSchemaDumped(outputPath);
            }
        }

        /// <summary>
        /// Parses all workshops from Excel package using event schema configuration.
        /// Loads attendees from ClassSelection sheet and processes each period sheet to build workshop roster.
        /// </summary>
        /// <param name="package">EPPlus ExcelPackage containing workshop registration data.</param>
        /// <returns>List of Workshop objects with associated participant selections.</returns>
        private List<Workshop> ParseWorkshops(ExcelPackage package)
        {
            try
            {
                _schema = LoadEventSchema();
                LogInformationSchemaLoaded(_schema.EventName);

                // Step 1: Load all attendees from ClassSelection sheet
                var attendees = LoadAttendees(package, _schema);
                LogInformationAttendeesLoaded(attendees.Count);

                if (attendees.Count == 0)
                {
                    LogWarningNoAttendeesFound();
                }

                // Step 2: Parse workshops from each period sheet defined in schema
                var allWorkshops = new List<Workshop>();
                foreach (var periodConfig in _schema.PeriodSheets)
                {
                    var sheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == periodConfig.SheetName);
                    if (sheet == null)
                    {
                        LogWarningPeriodSheetNotFound(periodConfig.SheetName);
                        continue;
                    }

                    LogDebugProcessingPeriodSheet(sheet.Name);
                    var workshops = CollectWorkshops(sheet, periodConfig, attendees);
                    allWorkshops.AddRange(workshops);
                    LogDebugFoundWorkshopsInSheet(workshops.Count, sheet.Name);
                }

                LogInformationTotalWorkshopsParsed(allWorkshops.Count);
                return allWorkshops;
            }
            catch (ExcelParsingException)
            {
                // Re-throw our custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                LogErrorFailedToParseWorkshops(ex);
                throw new ExcelParsingException("Failed to parse workshops. Please verify the Excel file structure matches the expected schema.", ex);
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
                    var availableSheets = package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
                    LogWarningClassSelectionSheetNotFound(sheetConfig.SheetName, string.Join(", ", availableSheets));

                    throw new MissingSheetException($"Required sheet '{sheetConfig.SheetName}' not found in Excel file.")
                    {
                        SheetName = sheetConfig.SheetName,
                        AvailableSheets = availableSheets
                    };
                }

                if (sheet.Dimension == null)
                {
                    LogWarningClassSelectionSheetEmpty(sheetConfig.SheetName);
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
                            LogDebugGeneratedFallbackAttendeeId($"{firstName} {lastName}", selectionId);
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
                        LogWarningFailedToParseAttendeeRow(ex, row, sheetConfig.SheetName);
                        // Continue processing other rows
                    }
                }

                return attendees;
            }
            catch (MissingSheetException)
            {
                // Re-throw our custom exception
                throw;
            }
            catch (Exception ex)
            {
                LogErrorFailedToLoadAttendees(ex);
                throw new ExcelParsingException("Failed to load attendees. Please verify the ClassSelection sheet structure.", ex)
                {
                    SheetName = schema.ClassSelectionSheet.SheetName
                };
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
                LogDebugSheetEmpty(sheet.Name);
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
                                    LogDebugSkippingEmptyWorkshopName(row, workshopCol.ColumnName);
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
                                LogWarningFailedToParseWorkshopData(ex, row, workshopCol.ColumnName, sheet.Name);
                                // Continue processing other workshop columns
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarningFailedToProcessRow(ex, row, sheet.Name);
                        // Continue processing other rows
                    }
                }

                return workshops.Values.ToList();
            }
            catch (Exception ex)
            {
                LogErrorFailedToCollectWorkshops(ex, sheet.Name);
                throw new ExcelParsingException($"Failed to collect workshops from sheet '{sheet.Name}'. Please verify the sheet structure.", ex)
                {
                    SheetName = sheet.Name
                };
            }
        }

        #region Logging

        // 2001-2050: ParseFromStream
        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Starting Excel import, stream size: {size} bytes"
        )]
        private partial void LogInformationStartingExcelImport(long size);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Debug,
            Message = "Excel package loaded with {count} worksheets"
        )]
        private partial void LogDebugExcelPackageLoaded(int count);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Information,
            Message = "Excel import completed successfully, {count} workshops parsed"
        )]
        private partial void LogInformationExcelImportCompleted(int count);

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Error,
            Message = "Failed to import Excel file"
        )]
        private partial void LogErrorFailedToImportExcel(Exception ex);

        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Information,
            Message = "Excel schema written to: {outputPath}"
        )]
        private partial void LogInformationExcelSchemaDumped(string outputPath);

        // 2051-2100: ParseWorkshops
        [LoggerMessage(
            EventId = 2051,
            Level = LogLevel.Information,
            Message = "Loaded schema for event: {eventName}"
        )]
        private partial void LogInformationSchemaLoaded(string eventName);

        [LoggerMessage(
            EventId = 2052,
            Level = LogLevel.Information,
            Message = "Loaded {count} attendees from ClassSelection sheet"
        )]
        private partial void LogInformationAttendeesLoaded(int count);

        [LoggerMessage(
            EventId = 2053,
            Level = LogLevel.Warning,
            Message = "No attendees found in ClassSelection sheet - workshop parsing may be incomplete"
        )]
        private partial void LogWarningNoAttendeesFound();

        [LoggerMessage(
            EventId = 2054,
            Level = LogLevel.Warning,
            Message = "Could not find period sheet: {sheetName}"
        )]
        private partial void LogWarningPeriodSheetNotFound(string sheetName);

        [LoggerMessage(
            EventId = 2055,
            Level = LogLevel.Debug,
            Message = "Processing period sheet: {sheetName}"
        )]
        private partial void LogDebugProcessingPeriodSheet(string sheetName);

        [LoggerMessage(
            EventId = 2056,
            Level = LogLevel.Debug,
            Message = "Found {count} workshops in {sheetName}"
        )]
        private partial void LogDebugFoundWorkshopsInSheet(int count, string sheetName);

        [LoggerMessage(
            EventId = 2057,
            Level = LogLevel.Information,
            Message = "Total workshops parsed: {count}"
        )]
        private partial void LogInformationTotalWorkshopsParsed(int count);

        [LoggerMessage(
            EventId = 2058,
            Level = LogLevel.Error,
            Message = "Failed to parse workshops from Excel package"
        )]
        private partial void LogErrorFailedToParseWorkshops(Exception ex);

        // 2101-2150: LoadAttendees
        [LoggerMessage(
            EventId = 2101,
            Level = LogLevel.Warning,
            Message = "ClassSelection sheet '{sheetName}' not found. Available sheets: {availableSheets}"
        )]
        private partial void LogWarningClassSelectionSheetNotFound(string sheetName, string availableSheets);

        [LoggerMessage(
            EventId = 2102,
            Level = LogLevel.Warning,
            Message = "ClassSelection sheet '{sheetName}' is empty"
        )]
        private partial void LogWarningClassSelectionSheetEmpty(string sheetName);

        [LoggerMessage(
            EventId = 2103,
            Level = LogLevel.Debug,
            Message = "Generated fallback ID for attendee: {fullName} -> {selectionId}"
        )]
        private partial void LogDebugGeneratedFallbackAttendeeId(string fullName, string selectionId);

        [LoggerMessage(
            EventId = 2104,
            Level = LogLevel.Warning,
            Message = "Failed to parse attendee data at row {row} in {sheetName}"
        )]
        private partial void LogWarningFailedToParseAttendeeRow(Exception ex, int row, string sheetName);

        [LoggerMessage(
            EventId = 2105,
            Level = LogLevel.Error,
            Message = "Failed to load attendees from ClassSelection sheet"
        )]
        private partial void LogErrorFailedToLoadAttendees(Exception ex);

        // 2151-2200: CollectWorkshops
        [LoggerMessage(
            EventId = 2151,
            Level = LogLevel.Debug,
            Message = "Sheet {sheetName} has no dimension (empty sheet)"
        )]
        private partial void LogDebugSheetEmpty(string sheetName);

        [LoggerMessage(
            EventId = 2152,
            Level = LogLevel.Debug,
            Message = "Skipping empty workshop name at row {row}, column {column}"
        )]
        private partial void LogDebugSkippingEmptyWorkshopName(int row, string column);

        [LoggerMessage(
            EventId = 2153,
            Level = LogLevel.Warning,
            Message = "Failed to parse workshop data at row {row}, column {column} in sheet {sheetName}"
        )]
        private partial void LogWarningFailedToParseWorkshopData(Exception ex, int row, string column, string sheetName);

        [LoggerMessage(
            EventId = 2154,
            Level = LogLevel.Warning,
            Message = "Failed to process row {row} in sheet {sheetName}"
        )]
        private partial void LogWarningFailedToProcessRow(Exception ex, int row, string sheetName);

        [LoggerMessage(
            EventId = 2155,
            Level = LogLevel.Error,
            Message = "Failed to collect workshops from sheet {sheetName}"
        )]
        private partial void LogErrorFailedToCollectWorkshops(Exception ex, string sheetName);

        #endregion
    }
}

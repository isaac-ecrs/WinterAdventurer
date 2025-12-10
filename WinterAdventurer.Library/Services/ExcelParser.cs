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
    public class ExcelParser
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

                _logger.LogInformation("Starting Excel import, stream size: {Size} bytes", stream.Length);

                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        throw new ExcelParsingException("Excel file contains no worksheets");
                    }

                    _logger.LogDebug("Excel package loaded with {Count} worksheets", package.Workbook.Worksheets.Count);
                    var workshops = ParseWorkshops(package);
                    _logger.LogInformation("Excel import completed successfully, {Count} workshops parsed", workshops.Count);
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
                _logger.LogError(ex, "Failed to import Excel file");
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
                _logger.LogInformation("Excel schema written to: {OutputPath}", outputPath);
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
            catch (ExcelParsingException)
            {
                // Re-throw our custom exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse workshops from Excel package");
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
                    _logger.LogWarning("ClassSelection sheet '{SheetName}' not found. Available sheets: {AvailableSheets}",
                        sheetConfig.SheetName, string.Join(", ", availableSheets));

                    throw new MissingSheetException($"Required sheet '{sheetConfig.SheetName}' not found in Excel file.")
                    {
                        SheetName = sheetConfig.SheetName,
                        AvailableSheets = availableSheets
                    };
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
                        {
                            continue;
                        }

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
            catch (MissingSheetException)
            {
                // Re-throw our custom exception
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load attendees from ClassSelection sheet");
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
                        {
                            choiceNumber = 1;
                        }

                        if (!int.TryParse(registrationIdStr, out int registrationId))
                        {
                            registrationId = 0;
                        }

                        // Process each workshop column configured for this period
                        foreach (var workshopCol in periodConfig.WorkshopColumns)
                        {
                            try
                            {
                                var cellValue = helper.GetCellValue(row, workshopCol.ColumnName);
                                if (string.IsNullOrWhiteSpace(cellValue))
                                {
                                    continue;
                                }

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
                throw new ExcelParsingException($"Failed to collect workshops from sheet '{sheet.Name}'. Please verify the sheet structure.", ex)
                {
                    SheetName = sheet.Name
                };
            }
        }
    }
}

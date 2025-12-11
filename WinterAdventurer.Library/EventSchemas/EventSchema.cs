using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinterAdventurer.Library.EventSchemas
{
    /// <summary>
    /// Root configuration for schema-driven Excel parsing in the WinterAdventurer system.
    /// Defines all Excel column mappings, worksheet structures, and parsing rules for a specific event.
    /// This enables the system to adapt to different event formats without code changes.
    /// Schema files are JSON documents stored in EventSchemas/ folder and embedded as resources.
    /// </summary>
    /// <example>
    /// Example schema usage:
    /// <code>
    /// {
    ///   "eventName": "Winter Adventure 2025",
    ///   "totalDays": 4,
    ///   "classSelectionSheet": { ... },
    ///   "periodSheets": [ ... ],
    ///   "workshopFormat": { ... }
    /// }
    /// </code>
    /// </example>
    public class EventSchema
    {
        /// <summary>
        /// Name of the event (e.g., "Winter Adventure 2025").
        /// Used for logging and identification purposes during Excel import.
        /// </summary>
        [JsonProperty("eventName")]
        public string EventName { get; set; } = string.Empty;

        /// <summary>
        /// Total number of days the event spans (e.g., 4 for a 4-day event).
        /// Used to determine day columns in individual schedules and master schedules.
        /// Workshops can run for the full duration or partial day ranges (e.g., Days 1-2, Days 3-4).
        /// </summary>
        [JsonProperty("totalDays")]
        public int TotalDays { get; set; }

        /// <summary>
        /// Configuration for the ClassSelection worksheet containing attendee registration data.
        /// Defines column mappings for participant information (name, email, age, registration ID).
        /// This sheet provides the master list of attendees that workshop selections are linked to.
        /// </summary>
        [JsonProperty("classSelectionSheet")]
        public ClassSelectionSheetConfig ClassSelectionSheet { get; set; } = new();

        /// <summary>
        /// Collection of period sheet configurations, one for each workshop time period.
        /// Each period represents a distinct time slot (e.g., "Morning First Period", "Afternoon Period").
        /// Period sheets contain workshop selection data organized by participant and day ranges.
        /// </summary>
        [JsonProperty("periodSheets")]
        public List<PeriodSheetConfig> PeriodSheets { get; set; } = new();

        /// <summary>
        /// Configuration for parsing workshop cell format in period sheets.
        /// Defines the pattern used to extract workshop names and leader names from Excel cells.
        /// Typical format is "Workshop Name (Leader Name)" where parentheses separate the two components.
        /// </summary>
        [JsonProperty("workshopFormat")]
        public WorkshopFormatConfig WorkshopFormat { get; set; } = new();
    }

    /// <summary>
    /// Configuration for the ClassSelection worksheet structure and column mappings.
    /// This sheet contains the master attendee roster with participant registration details.
    /// Column mappings support both exact column names and patterns for flexible matching.
    /// </summary>
    /// <example>
    /// Example configuration:
    /// <code>
    /// {
    ///   "sheetName": "ClassSelection",
    ///   "columns": {
    ///     "selectionId": { "pattern": "ClassSelection_Id" },
    ///     "firstName": "Name_First",
    ///     "lastName": "Name_Last",
    ///     "email": "Email",
    ///     "age": "Age"
    ///   }
    /// }
    /// </code>
    /// </example>
    public class ClassSelectionSheetConfig
    {
        /// <summary>
        /// Name of the Excel worksheet containing attendee registration data.
        /// Typically "ClassSelection" but can be customized for different event formats.
        /// </summary>
        [JsonProperty("sheetName")]
        public string SheetName { get; set; } = string.Empty;

        /// <summary>
        /// Dictionary mapping logical field names to Excel column headers.
        /// Keys are standard field identifiers (e.g., "firstName", "lastName", "selectionId").
        /// Values can be exact column names (strings) or pattern objects for flexible matching.
        /// Pattern objects support substring matching for columns that vary by year (e.g., "2024ClassSelection_Id").
        /// </summary>
        [JsonProperty("columns")]
        public Dictionary<string, object> Columns { get; set; } = new();

        /// <summary>
        /// Retrieves column name from schema configuration, supporting both exact string values and pattern objects.
        /// Enables flexible Excel column mapping where columns may be named consistently or vary by year/event.
        /// </summary>
        /// <param name="key">Configuration key (e.g., "firstName", "registrationId") to look up.</param>
        /// <returns>Column name string (exact or pattern), or empty string if key not found or value is invalid.</returns>
        public string GetColumnName(string key)
        {
            if (!Columns.ContainsKey(key))
                return string.Empty;

            var value = Columns[key];

            // If it's a simple string, return it
            if (value is string str)
                return str;

            // If it's a pattern object, extract the pattern
            if (value is Newtonsoft.Json.Linq.JObject obj && obj["pattern"] != null)
            {
                return obj["pattern"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Configuration for a period worksheet containing workshop selections for a specific time slot.
    /// Each period sheet represents one workshop time period during the event (e.g., Morning First Period).
    /// Contains column mappings for participant identification and workshop column definitions for different day ranges.
    /// </summary>
    /// <example>
    /// Example configuration:
    /// <code>
    /// {
    ///   "sheetName": "MorningFirstPeriod",
    ///   "displayName": "Morning First Period",
    ///   "columns": {
    ///     "selectionId": { "pattern": "ClassSelection_Id" },
    ///     "choiceNumber": "ChoiceNumber",
    ///     "firstName": "Name_First",
    ///     "lastName": "Name_Last"
    ///   },
    ///   "workshopColumns": [
    ///     { "columnName": "Class4Day", "startDay": 1, "endDay": 4 },
    ///     { "columnName": "ClassFirstTwoDays", "startDay": 1, "endDay": 2 }
    ///   ]
    /// }
    /// </code>
    /// </example>
    public class PeriodSheetConfig
    {
        /// <summary>
        /// Name of the Excel worksheet for this period (e.g., "MorningFirstPeriod").
        /// Used to locate the worksheet during parsing and as part of the unique workshop identifier.
        /// </summary>
        [JsonProperty("sheetName")]
        public string SheetName { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable display name for this period (e.g., "Morning First Period").
        /// Used in PDF generation for schedule headers and time slot labels.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Dictionary mapping logical field names to Excel column headers for participant identification.
        /// Keys include "selectionId", "choiceNumber", "firstName", "lastName", "registrationId".
        /// Values can be exact column names (strings) or pattern objects for flexible matching.
        /// Used to link workshop selections back to attendee records from ClassSelection sheet.
        /// </summary>
        [JsonProperty("columns")]
        public Dictionary<string, object> Columns { get; set; } = new();

        /// <summary>
        /// List of workshop column configurations defining duration variants for this period.
        /// Each configuration maps an Excel column to a specific day range (e.g., Days 1-4, Days 1-2, Days 3-4).
        /// Allows workshops to run for full event duration or split into shorter sessions.
        /// The same workshop name in different duration columns creates distinct workshop offerings.
        /// </summary>
        [JsonProperty("workshopColumns")]
        public List<WorkshopColumnConfig> WorkshopColumns { get; set; } = new();

        /// <summary>
        /// Retrieves column name from period sheet schema configuration, supporting both exact string values and pattern objects.
        /// Enables flexible Excel column mapping where columns may be named consistently or vary by year/event.
        /// </summary>
        /// <param name="key">Configuration key (e.g., "selectionId", "choiceNumber") to look up.</param>
        /// <returns>Column name string (exact or pattern), or empty string if key not found or value is invalid.</returns>
        public string GetColumnName(string key)
        {
            if (!Columns.ContainsKey(key))
            {
                return string.Empty;
            }
            var value = Columns[key];

            if (value is string str)
            {
                return str;
            }

            if (value is Newtonsoft.Json.Linq.JObject obj && obj["pattern"] != null)
            {
                return obj["pattern"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Configuration for a single workshop column within a period sheet.
    /// Maps an Excel column to a specific day range, enabling workshops with different durations.
    /// Multiple workshop columns in the same period allow participants to choose between
    /// full-duration workshops (e.g., 4-day) and split-duration workshops (e.g., 2-day).
    /// </summary>
    /// <example>
    /// Example configurations:
    /// <code>
    /// // 4-day workshop (full event)
    /// { "columnName": "Class4Day", "startDay": 1, "endDay": 4 }
    ///
    /// // First 2-day workshop
    /// { "columnName": "ClassFirstTwoDays", "startDay": 1, "endDay": 2 }
    ///
    /// // Second 2-day workshop
    /// { "columnName": "ClassSecondTwoDays", "startDay": 3, "endDay": 4 }
    /// </code>
    /// </example>
    public class WorkshopColumnConfig
    {
        /// <summary>
        /// Name of the Excel column containing workshop selections for this duration.
        /// Example values: "Class4Day", "ClassFirstTwoDays", "ClassSecondTwoDays".
        /// Must match the exact column header in the period worksheet.
        /// </summary>
        [JsonProperty("columnName")]
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Starting day number for this workshop duration (1-indexed).
        /// For a 4-day event, valid values are 1-4.
        /// Combined with EndDay to create the WorkshopDuration object.
        /// </summary>
        [JsonProperty("startDay")]
        public int StartDay { get; set; }

        /// <summary>
        /// Ending day number for this workshop duration (1-indexed).
        /// For a 4-day event, valid values are 1-4.
        /// Must be greater than or equal to StartDay.
        /// Example: StartDay=1, EndDay=2 represents a workshop running Days 1-2.
        /// </summary>
        [JsonProperty("endDay")]
        public int EndDay { get; set; }
    }

    /// <summary>
    /// Configuration for parsing workshop information from Excel cell values.
    /// Defines the text pattern used to extract workshop name and leader name from combined cell values.
    /// The parsing logic is implemented in StringExtensions (GetWorkshopName, GetLeaderName methods).
    /// </summary>
    /// <example>
    /// Example configuration:
    /// <code>
    /// {
    ///   "pattern": "WorkshopName (LeaderName)",
    ///   "description": "Workshop name followed by leader name in parentheses"
    /// }
    /// </code>
    /// This configuration would parse "Pottery (Jane Smith)" as:
    /// - Workshop Name: "Pottery"
    /// - Leader Name: "Jane Smith"
    /// </example>
    public class WorkshopFormatConfig
    {
        /// <summary>
        /// Template pattern showing how workshop and leader information is formatted in Excel cells.
        /// Standard pattern is "WorkshopName (LeaderName)" where parentheses delimit the leader name.
        /// Used primarily for documentation and validation purposes.
        /// Actual parsing is handled by StringExtensions.GetWorkshopName() and GetLeaderName().
        /// </summary>
        [JsonProperty("pattern")]
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the workshop format pattern.
        /// Explains how workshop and leader information should be structured in the Excel file.
        /// Used for documentation and error messages when parsing fails.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
    }
}

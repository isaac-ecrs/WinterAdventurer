using Newtonsoft.Json;
using System.Collections.Generic;

namespace WinterAdventurer.Library.EventSchemas
{
    public class EventSchema
    {
        [JsonProperty("eventName")]
        public string EventName { get; set; } = string.Empty;

        [JsonProperty("totalDays")]
        public int TotalDays { get; set; }

        [JsonProperty("classSelectionSheet")]
        public ClassSelectionSheetConfig ClassSelectionSheet { get; set; } = new();

        [JsonProperty("periodSheets")]
        public List<PeriodSheetConfig> PeriodSheets { get; set; } = new();

        [JsonProperty("workshopFormat")]
        public WorkshopFormatConfig WorkshopFormat { get; set; } = new();
    }

    public class ClassSelectionSheetConfig
    {
        [JsonProperty("sheetName")]
        public string SheetName { get; set; } = string.Empty;

        [JsonProperty("columns")]
        public Dictionary<string, object> Columns { get; set; } = new();

        public string GetColumnName(string key)
        {
            if (!Columns.ContainsKey(key)) return string.Empty;

            var value = Columns[key];

            // If it's a simple string, return it
            if (value is string str) return str;

            // If it's a pattern object, extract the pattern
            if (value is Newtonsoft.Json.Linq.JObject obj && obj["pattern"] != null)
            {
                return obj["pattern"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }

    public class PeriodSheetConfig
    {
        [JsonProperty("sheetName")]
        public string SheetName { get; set; } = string.Empty;

        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("columns")]
        public Dictionary<string, object> Columns { get; set; } = new();

        [JsonProperty("workshopColumns")]
        public List<WorkshopColumnConfig> WorkshopColumns { get; set; } = new();

        public string GetColumnName(string key)
        {
            if (!Columns.ContainsKey(key)) return string.Empty;

            var value = Columns[key];

            if (value is string str) return str;

            if (value is Newtonsoft.Json.Linq.JObject obj && obj["pattern"] != null)
            {
                return obj["pattern"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }

    public class WorkshopColumnConfig
    {
        [JsonProperty("columnName")]
        public string ColumnName { get; set; } = string.Empty;

        [JsonProperty("startDay")]
        public int StartDay { get; set; }

        [JsonProperty("endDay")]
        public int EndDay { get; set; }
    }

    public class WorkshopFormatConfig
    {
        [JsonProperty("pattern")]
        public string Pattern { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
    }
}

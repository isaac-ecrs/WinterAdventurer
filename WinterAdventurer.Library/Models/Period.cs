using System.Text.RegularExpressions;

namespace WinterAdventurer.Library.Models
{
    /// <summary>
    /// Represents a time period during which workshops are offered (e.g., "Morning First Period", "Afternoon Second Period").
    /// Each period corresponds to an Excel sheet containing workshop selections for that time slot.
    /// </summary>
    public class Period
    {
        /// <summary>
        /// Gets or sets the technical Excel sheet name for this period (e.g., "MorningFirstPeriod").
        /// Used for matching Excel sheets during import.
        /// </summary>
        public string SheetName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user-friendly display name for this period (e.g., "Morning First Period").
        /// Generated automatically from SheetName by inserting spaces before capital letters.
        /// Used in schedules and reports.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Creates a period from an Excel sheet name, generating a user-friendly display name.
        /// Display name conversion enables showing "Morning First Period" instead of "MorningFirstPeriod" in schedules and reports.
        /// </summary>
        /// <param name="sheetName">Technical Excel sheet name (e.g., "MorningFirstPeriod").</param>
        public Period(string sheetName)
        {
            SheetName = sheetName;
            DisplayName = Regex.Replace(SheetName, "(?<!^)([A-Z])", " $1");
        }
    }
}

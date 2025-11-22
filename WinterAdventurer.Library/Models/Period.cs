using System.Text.RegularExpressions;

namespace WinterAdventurer.Library.Models
{
    public class Period
    {
        public string SheetName {get;set;} = string.Empty;
        public string DisplayName {get;set;} = string.Empty;

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
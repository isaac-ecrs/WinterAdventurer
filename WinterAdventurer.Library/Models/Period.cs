using System.Text.RegularExpressions;

namespace WinterAdventurer.Library.Models
{
    public class Period
    {
        public string SheetName {get;set;} = string.Empty;
        public string DisplayName {get;set;} = string.Empty;

        public Period(string sheetName)
        {
            SheetName = sheetName;
            DisplayName = Regex.Replace(SheetName, "(?<!^)([A-Z])", " $1");
        }
    }
}
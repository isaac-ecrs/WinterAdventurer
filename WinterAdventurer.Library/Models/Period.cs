using System.Text.RegularExpressions;

namespace WinterAdventurer.Library.Models
{
    public class Period
    {
        public string SheetName {get;set;}
        public string DisplayName {get;set;}

        public Period(string sheetName)
        {
            SheetName = sheetName;
            DisplayName = Regex.Replace(SheetName, "(?<!^)([A-Z])", " $1");
        }
    }
}
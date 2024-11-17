using WinterAdventurer.Library.Extensions;

namespace WinterAdventurer.Library.Models
{
    public class PersonName
    {
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public PersonName(string fullName)
        {
            var splitName = fullName.Split(' ');
            FullName = fullName.ToProper();
            FirstName = splitName[0].ToProper();
            LastName = fullName.Replace(splitName[0],"").Trim().ToProper();
        }
    }
}
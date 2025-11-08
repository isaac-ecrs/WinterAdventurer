using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WinterAdventurer.Library.Models
{
    public class WorkshopSelection
    {
        public string ClassSelectionId { get; set; } = string.Empty;
        public string WorkshopName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int ChoiceNumber { get; set; }
        public WorkshopDuration Duration { get; set; } = new WorkshopDuration(1, 1);
        public int RegistrationId { get; set; }  // For chronological sorting

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

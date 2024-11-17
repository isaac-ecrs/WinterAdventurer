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
        public int RegistrationId { get; set; }
        public int SelectionId { get; set; }
        public string ClassName { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

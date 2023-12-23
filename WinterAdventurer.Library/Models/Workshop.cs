using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WinterAdventurer.Library.Models
{
    public class Workshop
    {
        public string Name { get; set; }
        public string Leader { get; set; }
        public string Type { get; set; }
        public int MaxParticipants { get; set; }
        public int MinAge { get; set; }
        public List<WorkshopSelection> Selections { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }    
}

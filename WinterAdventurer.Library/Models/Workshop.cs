using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WinterAdventurer.Library.Models
{
    public class Workshop
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Location { get; set; } = string.Empty;
        [Required]
        public Period Period { get; set; }
        [Required]
        public WorkshopDuration Duration { get; set; } = new WorkshopDuration(1, 1);
        [Required]
        public string Leader { get; set; } = string.Empty;
        public bool IsMini { get; set; } = false;
        public int MaxParticipants { get; set; } = 0;
        public int MinAge { get; set; } = 0;
        public List<WorkshopSelection> Selections { get; set; } = new List<WorkshopSelection>();

        // Unique key for this workshop offering
        public string Key => $"{Period.SheetName}|{Name}|{Leader}|{Duration.StartDay}-{Duration.EndDay}";

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }    
}

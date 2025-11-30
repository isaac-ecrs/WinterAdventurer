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
        public Period Period { get; set; } = new Period(string.Empty);
        [Required]
        public WorkshopDuration Duration { get; set; } = new WorkshopDuration(1, 1);
        [Required]
        public string Leader { get; set; } = string.Empty;
        public bool IsMini { get; set; } = false;
        public int MaxParticipants { get; set; } = 0;
        public int MinAge { get; set; } = 0;
        public List<WorkshopSelection> Selections { get; set; } = new List<WorkshopSelection>();

        /// <summary>
        /// Location tags (e.g., "Downstairs") for display in participant schedules.
        /// Used to highlight special location characteristics like floor level.
        /// </summary>
        public List<LocationTag>? Tags { get; set; } = null;

        // Unique key for this workshop offering
        public string Key => $"{Period.SheetName}|{Name}|{Leader}|{Duration.StartDay}-{Duration.EndDay}";

        /// <summary>
        /// Serializes workshop data to JSON for debugging and logging purposes.
        /// Enables quick inspection of workshop state including all participant selections during aggregation and PDF generation.
        /// </summary>
        /// <returns>JSON representation of workshop with all properties and nested selections.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }    
}

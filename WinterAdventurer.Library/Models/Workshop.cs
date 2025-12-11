using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WinterAdventurer.Library.Models
{
    /// <summary>
    /// Represents a workshop offering during a specific period and duration.
    /// Workshops aggregate multiple participant selections and include metadata like location, leader, and capacity constraints.
    /// The same workshop name can appear in multiple periods or with different durations, each treated as a separate workshop.
    /// </summary>
    public class Workshop
    {
        /// <summary>
        /// Gets or sets the workshop name (e.g., "Pottery", "Woodworking").
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the physical location where the workshop is held (e.g., "Art Studio", "Workshop Room").
        /// Used for master schedule generation and participant wayfinding.
        /// </summary>
        [Required]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time period during which this workshop is offered (e.g., "Morning First Period").
        /// </summary>
        [Required]
        public Period Period { get; set; } = new Period(string.Empty);

        /// <summary>
        /// Gets or sets the duration of the workshop (which days it runs during the event).
        /// </summary>
        [Required]
        public WorkshopDuration Duration { get; set; } = new WorkshopDuration(1, 1);

        /// <summary>
        /// Gets or sets the workshop leader's name.
        /// Part of the unique identifier for workshops (same workshop name can have different leaders).
        /// </summary>
        [Required]
        public string Leader { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is a mini workshop (shorter duration or special format).
        /// </summary>
        public bool IsMini { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of participants allowed in this workshop.
        /// Used for capacity planning but not enforced during import.
        /// </summary>
        public int MaxParticipants { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum age requirement for participants.
        /// Used for reference but not enforced during import.
        /// </summary>
        public int MinAge { get; set; } = 0;

        /// <summary>
        /// Gets or sets the list of participant selections for this workshop.
        /// Includes both first-choice enrollments (ChoiceNumber=1) and backup/alternate selections (ChoiceNumber>1).
        /// </summary>
        public List<WorkshopSelection> Selections { get; set; } = new List<WorkshopSelection>();

        /// <summary>
        /// Location tags (e.g., "Downstairs") for display in participant schedules.
        /// Used to highlight special location characteristics like floor level.
        /// </summary>
        public List<LocationTag>? Tags { get; set; } = null;

        /// <summary>
        /// Gets the unique identifier for this workshop offering.
        /// Format: "{Period}|{Name}|{Leader}|{StartDay}-{EndDay}".
        /// Ensures the same workshop with different periods, leaders, or durations are treated as separate offerings.
        /// </summary>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WinterAdventurer.Library.Models
{
    /// <summary>
    /// Represents a participant's selection of a workshop during a specific period.
    /// Each selection includes the attendee information, workshop choice preference, and duration.
    /// Multiple selections are aggregated into Workshop objects during import.
    /// </summary>
    public class WorkshopSelection
    {
        /// <summary>
        /// Gets or sets the unique identifier linking this selection to an attendee record.
        /// Used to correlate workshop choices with attendee information from the ClassSelection sheet.
        /// </summary>
        public string ClassSelectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the selected workshop.
        /// </summary>
        public string WorkshopName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the participant's full name in "FirstName LastName" format.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the participant's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the participant's last name.
        /// Used for alphabetical sorting in class rosters.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the participant's choice preference number.
        /// 1 indicates first choice (enrolled), 2+ indicates backup/alternate choices.
        /// </summary>
        public int ChoiceNumber { get; set; }

        /// <summary>
        /// Gets or sets the duration of the workshop (which days it runs).
        /// </summary>
        public WorkshopDuration Duration { get; set; } = new WorkshopDuration(1, 1);

        /// <summary>
        /// Gets or sets the registration ID for chronological sorting.
        /// Used to maintain registration order when displaying participants who registered at different times.
        /// </summary>
        public int RegistrationId { get; set; }

        /// <summary>
        /// Serializes workshop selection to JSON for debugging and logging purposes.
        /// Enables quick inspection of selection state during Excel parsing and workshop aggregation.
        /// </summary>
        /// <returns>JSON representation of workshop selection with all properties.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

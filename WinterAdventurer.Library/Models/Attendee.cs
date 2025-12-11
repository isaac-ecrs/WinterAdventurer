// <copyright file="Attendee.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Newtonsoft.Json;

namespace WinterAdventurer.Library.Models
{
    /// <summary>
    /// Represents a participant in the workshop registration system.
    /// Attendees are imported from Excel and matched with their workshop selections.
    /// </summary>
    public class Attendee
    {
        /// <summary>
        /// Gets or sets the unique identifier for this attendee from the Excel ClassSelection sheet.
        /// Used to correlate attendee records with workshop selection records across multiple Excel sheets.
        /// </summary>
        public string ClassSelectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attendee's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attendee's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the attendee's full name in "FirstName LastName" format.
        /// Used for display in schedules and rosters.
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Gets or sets the attendee's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attendee's age.
        /// Stored as string to accommodate age ranges (e.g., "12-14") from Excel data.
        /// </summary>
        public string Age { get; set; } = string.Empty;

        /// <summary>
        /// Serializes attendee data to JSON for debugging and logging purposes.
        /// Enables quick inspection of attendee state during Excel parsing and PDF generation.
        /// </summary>
        /// <returns>JSON representation of attendee with all properties.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

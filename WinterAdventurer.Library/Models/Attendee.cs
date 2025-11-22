using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinterAdventurer.Library.Models
{
    public class Attendee
    {
        public string ClassSelectionId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
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

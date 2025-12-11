namespace WinterAdventurer.Library.Models
{
    /// <summary>
    /// Represents a time slot in the event schedule, either for workshops or other activities (meals, free time, etc.).
    /// Time slots define when activities occur and are displayed on individual participant schedules.
    /// </summary>
    public class TimeSlot
    {
        /// <summary>
        /// Gets or sets the unique identifier for this time slot.
        /// Automatically generated GUID used for tracking and UI binding.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the display label for this time slot (e.g., "Morning First Period", "Lunch", "Free Time").
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start time of this slot.
        /// Null indicates time is not yet configured.
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of this slot.
        /// Null indicates time is not yet configured or is open-ended.
        /// </summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this time slot represents a workshop period.
        /// Period slots correspond to workshop sheets in the Excel import and appear in individual schedules.
        /// Non-period slots (meals, activities) are custom additions that don't come from Excel.
        /// </summary>
        public bool IsPeriod { get; set; } = false;

        /// <summary>
        /// Formats time slot as readable 12-hour time range for display in schedules.
        /// Supports both fixed-duration activities and open-ended time slots (displays "?" for unknown end time).
        /// </summary>
        public string TimeRange
        {
            get
            {
                if (StartTime.HasValue && EndTime.HasValue)
                {
                    var startDateTime = DateTime.Today.Add(StartTime.Value);
                    var endDateTime = DateTime.Today.Add(EndTime.Value);
                    return $"{startDateTime:h:mm tt} - {endDateTime:h:mm tt}";
                }
                else if (StartTime.HasValue && !EndTime.HasValue)
                {
                    var startDateTime = DateTime.Today.Add(StartTime.Value);
                    return $"{startDateTime:h:mm tt} - ?";
                }

                return string.Empty;
            }
        }
    }
}

namespace WinterAdventurer.Library.Models
{
    public class TimeSlot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Label { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
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
                return "";
            }
        }
    }
}

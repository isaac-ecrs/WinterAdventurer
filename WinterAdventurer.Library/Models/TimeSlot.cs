namespace WinterAdventurer.Library.Models
{
    public class TimeSlot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Label { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsPeriod { get; set; } = false;

        public string TimeRange
        {
            get
            {
                if (StartTime.HasValue && EndTime.HasValue)
                {
                    // Convert TimeSpan to formatted time string (12-hour format with AM/PM)
                    var startDateTime = DateTime.Today.Add(StartTime.Value);
                    var endDateTime = DateTime.Today.Add(EndTime.Value);
                    return $"{startDateTime:h:mm tt} - {endDateTime:h:mm tt}";
                }
                else if (StartTime.HasValue && !EndTime.HasValue)
                {
                    // Open-ended timeslot (e.g., "Late Night Activities")
                    var startDateTime = DateTime.Today.Add(StartTime.Value);
                    return $"{startDateTime:h:mm tt} - ?";
                }
                return "";
            }
        }
    }
}

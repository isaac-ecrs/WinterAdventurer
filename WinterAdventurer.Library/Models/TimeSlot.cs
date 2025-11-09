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
                    return $"{StartTime.Value:hh\\:mm tt} - {EndTime.Value:hh\\:mm tt}";
                }
                return "";
            }
        }
    }
}

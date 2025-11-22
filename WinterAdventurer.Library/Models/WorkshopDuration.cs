namespace WinterAdventurer.Library.Models
{
    public class WorkshopDuration
    {
        public int StartDay { get; set; }
        public int EndDay { get; set; }

        public int NumberOfDays => EndDay - StartDay + 1;

        public string Description => NumberOfDays == 1
            ? $"Day {StartDay}"
            : $"Days {StartDay}-{EndDay}";

        /// <summary>
        /// Creates a workshop duration representing which days the workshop runs during the event.
        /// Enables tracking workshops that run full-event (days 1-4) versus half-event (days 1-2 or 3-4).
        /// </summary>
        /// <param name="startDay">First day of the workshop (1-based).</param>
        /// <param name="endDay">Last day of the workshop (1-based, inclusive).</param>
        public WorkshopDuration(int startDay, int endDay)
        {
            StartDay = startDay;
            EndDay = endDay;
        }
    }
}

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

        public WorkshopDuration(int startDay, int endDay)
        {
            StartDay = startDay;
            EndDay = endDay;
        }
    }
}

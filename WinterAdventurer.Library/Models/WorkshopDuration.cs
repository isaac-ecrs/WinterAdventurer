// <copyright file="WorkshopDuration.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Library.Models
{
    /// <summary>
    /// Represents the duration of a workshop within a multi-day event.
    /// Workshops can run for the full event (e.g., days 1-4) or partial event (e.g., days 1-2, days 3-4).
    /// </summary>
    public class WorkshopDuration
    {
        /// <summary>
        /// Gets or sets the first day of the workshop (1-based indexing).
        /// </summary>
        public int StartDay { get; set; }

        /// <summary>
        /// Gets or sets the last day of the workshop (1-based indexing, inclusive).
        /// </summary>
        public int EndDay { get; set; }

        /// <summary>
        /// Gets the total number of days the workshop runs.
        /// Calculated as (EndDay - StartDay + 1).
        /// </summary>
        public int NumberOfDays => EndDay - StartDay + 1;

        /// <summary>
        /// Gets a human-readable description of the workshop duration.
        /// Returns "Day X" for single-day workshops or "Days X-Y" for multi-day workshops.
        /// </summary>
        public string Description => NumberOfDays == 1
            ? $"Day {StartDay}"
            : $"Days {StartDay}-{EndDay}";

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkshopDuration"/> class.
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

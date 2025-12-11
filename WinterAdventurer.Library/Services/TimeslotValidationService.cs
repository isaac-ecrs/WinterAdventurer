// <copyright file="TimeslotValidationService.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Library.Services;

public interface ITimeslotValidationService
{
    /// <summary>
    /// Validates event schedule time slots to ensure no overlaps and all required periods are configured.
    /// Prevents schedule conflicts that would make it impossible to assign workshops to time periods.
    /// </summary>
    /// <param name="timeslots">Collection of time slots to validate (periods and custom activities).</param>
    /// <returns>Validation result indicating whether schedule is usable for workshop assignment.</returns>
    ValidationResult ValidateTimeslots(IEnumerable<TimeSlotDto> timeslots);
}

public class TimeslotValidationService : ITimeslotValidationService
{
    /// <summary>
    /// Validates event schedule time slots to ensure no overlaps and all required periods are configured.
    /// Prevents schedule conflicts that would make it impossible to assign workshops to time periods.
    /// </summary>
    /// <param name="timeslots">Collection of time slots to validate (periods and custom activities).</param>
    /// <returns>Validation result indicating whether schedule is usable for workshop assignment.</returns>
    public ValidationResult ValidateTimeslots(IEnumerable<TimeSlotDto> timeslots)
    {
        var result = new ValidationResult();
        var sortedTimeslots = timeslots
            .OrderBy(t => t.StartTime ?? TimeSpan.MaxValue)
            .ToList();

        // Check for unconfigured period timeslots (must have both times)
        // Non-period timeslots (custom activities) can remain unconfigured
        foreach (var timeslot in sortedTimeslots.Where(t => t.IsPeriod))
        {
            if (!timeslot.StartTime.HasValue || !timeslot.EndTime.HasValue)
            {
                result.HasUnconfiguredTimeslots = true;
                break;
            }
        }

        // Check for overlaps and duplicate start times
        for (int i = 0; i < sortedTimeslots.Count - 1; i++)
        {
            var current = sortedTimeslots[i];
            var next = sortedTimeslots[i + 1];

            // Check for identical or overlapping start times
            if (current.StartTime.HasValue && next.StartTime.HasValue)
            {
                // If two timeslots have the same start time, that's an overlap/duplicate
                if (current.StartTime == next.StartTime)
                {
                    result.HasOverlappingTimeslots = true;
                    break;
                }

                // If both have end times, check for traditional overlap
                if (current.EndTime.HasValue && next.EndTime.HasValue)
                {
                    // Check if current end time is after next start time
                    if (current.EndTime > next.StartTime)
                    {
                        result.HasOverlappingTimeslots = true;
                        break;
                    }
                }
            }
        }

        return result;
    }
}

public class TimeSlotDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool IsPeriod { get; set; }
}

public class ValidationResult
{
    public bool HasOverlappingTimeslots { get; set; }
    public bool HasUnconfiguredTimeslots { get; set; }
    public bool IsValid => !HasOverlappingTimeslots && !HasUnconfiguredTimeslots;
}

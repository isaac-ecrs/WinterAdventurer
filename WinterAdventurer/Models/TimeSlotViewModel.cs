// <copyright file="TimeSlotViewModel.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Models;

/// <summary>
/// View model for a timeslot in the schedule configuration.
/// Represents either a period (from Excel) or a custom activity (user-added).
/// </summary>
public class TimeSlotViewModel
{
    /// <summary>
    /// Gets or sets unique identifier for the timeslot.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets display label for the timeslot (e.g., "Morning First Period", "Lunch", "Break").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets start time of the timeslot.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Gets or sets end time of the timeslot.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether this is a period from the Excel file (true) or a custom user-added timeslot (false).
    /// Period timeslots cannot be deleted or renamed, and must have both start and end times configured.
    /// </summary>
    public bool IsPeriod { get; set; } = false;
}

// <copyright file="TimeslotOperationResult.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.Services
{
    /// <summary>
    /// Result of a timeslot save operation.
    /// </summary>
    public class TimeslotOperationResult
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public bool HasOverlappingTimeslots { get; set; }

        public bool HasUnconfiguredTimeslots { get; set; }
    }
}

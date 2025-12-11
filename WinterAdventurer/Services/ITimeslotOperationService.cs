// <copyright file="ITimeslotOperationService.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;
using WinterAdventurer.Models;
using LibraryTimeSlot = WinterAdventurer.Library.Models.TimeSlot;

namespace WinterAdventurer.Services
{
    /// <summary>
    /// Encapsulates timeslot operations (sorting, validation, conversion).
    /// Provides testable functions for timeslot management logic.
    /// </summary>
    public interface ITimeslotOperationService
    {
        /// <summary>
        /// Populates timeslots from periods, preserving user-added custom slots.
        /// </summary>
        List<TimeSlotViewModel> PopulateTimeslotsFromPeriods(
            List<Period> periods,
            List<TimeSlotViewModel> existingTimeslots);

        /// <summary>
        /// Sorts timeslots in-place by start time (nulls last).
        /// </summary>
        void SortTimeslots(List<TimeSlotViewModel> timeslots);

        /// <summary>
        /// Validates timeslots for overlaps and missing configurations.
        /// </summary>
#pragma warning disable CA1021 // Avoid 'out' parameters
        void ValidateTimeslots(
            List<TimeSlotViewModel> timeslots,
            ITimeslotValidationService validator,
            out bool hasOverlapping,
            out bool hasUnconfigured);
#pragma warning restore CA1021

        /// <summary>
        /// Converts UI TimeSlotViewModel list to Library TimeSlot format.
        /// </summary>
        List<LibraryTimeSlot> ConvertToLibraryTimeSlots(List<TimeSlotViewModel> viewModels);

        /// <summary>
        /// Saves timeslots to database and validates.
        /// </summary>
        Task<TimeslotOperationResult> SaveTimeslotsAsync(
            List<TimeSlotViewModel> timeslots,
            ILocationService locationService,
            ITimeslotValidationService validator);
    }
}

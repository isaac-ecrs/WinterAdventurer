// <copyright file="IHomeStateService.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Data;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Models;

namespace WinterAdventurer.Services
{
    /// <summary>
    /// Manages Home component state independently of Blazor component lifecycle.
    /// Enables state verification and mutation testing without component rendering.
    /// </summary>
    public interface IHomeStateService
    {
        // Workshop state
        IReadOnlyList<Workshop> Workshops { get; }

        List<Period> Periods { get; }

        // Timeslot state
        IReadOnlyList<TimeSlotViewModel> Timeslots { get; }

        bool HasOverlappingTimeslots { get; }

        bool HasUnconfiguredTimeslots { get; }

        // Location state
        IReadOnlyList<Location> AvailableLocations { get; }

        // Event metadata
        string EventName { get; set; }

        string DefaultEventName { get; set; }

        int BlankScheduleCount { get; set; }

        // Loading/UI state
        bool IsLoading { get; set; }

        bool Success { get; set; }

        bool ShowPdfSuccess { get; set; }

        string? ErrorMessage { get; set; }

        int TimeslotVersion { get; }

        int LocationListVersion { get; }

        // State mutation methods
        void ClearAll();

        void SetWorkshops(List<Workshop> workshops);

        void SetPeriods(List<Period> periods);

        void SetTimeslots(List<TimeSlotViewModel> timeslots);

        void UpdateTimeslotValidation(bool hasOverlapping, bool hasUnconfigured);

        void IncrementTimeslotVersion();

        void IncrementLocationListVersion();

        void AddLocation(Location location);

        void RemoveLocation(string name);
    }
}

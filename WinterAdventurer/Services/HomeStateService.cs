// <copyright file="HomeStateService.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Data;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Models;

namespace WinterAdventurer.Services
{
    /// <summary>
    /// Default implementation of IHomeStateService.
    /// Provides centralized state management for Home component.
    /// </summary>
    public class HomeStateService : IHomeStateService
    {
        private List<Workshop> _workshops = new ();
        private List<Period> _periods = new ();
        private List<TimeSlotViewModel> _timeslots = new ();
        private List<Location> _availableLocations = new ();
        private int _timeslotVersion = 1;
        private int _locationListVersion = 0;

        public IReadOnlyList<Workshop> Workshops => _workshops.AsReadOnly();

        public List<Period> Periods
        {
            get => _periods;
            private set => _periods = value;
        }

        public IReadOnlyList<TimeSlotViewModel> Timeslots => _timeslots.AsReadOnly();

        public bool HasOverlappingTimeslots { get; private set; }

        public bool HasUnconfiguredTimeslots { get; private set; }

        public IReadOnlyList<Location> AvailableLocations => _availableLocations.AsReadOnly();

        public string EventName { get; set; } = string.Empty;

        public string DefaultEventName { get; set; } = string.Empty;

        public int BlankScheduleCount { get; set; }

        public bool IsLoading { get; set; }

        public bool Success { get; set; }

        public bool ShowPdfSuccess { get; set; }

        public string? ErrorMessage { get; set; }

        public int TimeslotVersion => _timeslotVersion;

        public int LocationListVersion => _locationListVersion;

        public void ClearAll()
        {
            _workshops.Clear();
            _periods.Clear();
            _timeslots.Clear();
            _availableLocations.Clear();
            HasOverlappingTimeslots = false;
            HasUnconfiguredTimeslots = false;
            ErrorMessage = null;
            IsLoading = false;
            ShowPdfSuccess = false;
        }

        public void SetWorkshops(List<Workshop> workshops)
        {
            _workshops = workshops ?? new List<Workshop>();
        }

        public void SetPeriods(List<Period> periods)
        {
            _periods = periods ?? new List<Period>();
        }

        public void SetTimeslots(List<TimeSlotViewModel> timeslots)
        {
            _timeslots = timeslots ?? new List<TimeSlotViewModel>();
        }

        public void UpdateTimeslotValidation(bool hasOverlapping, bool hasUnconfigured)
        {
            HasOverlappingTimeslots = hasOverlapping;
            HasUnconfiguredTimeslots = hasUnconfigured;
        }

        public void IncrementTimeslotVersion()
        {
            _timeslotVersion++;
        }

        public void IncrementLocationListVersion()
        {
            _locationListVersion++;
        }

        public void AddLocation(Location location)
        {
            if (location != null && !_availableLocations.Any(l => l.Name == location.Name))
            {
                _availableLocations.Add(location);
                _availableLocations = _availableLocations.OrderBy(l => l.Name).ToList();
            }
        }

        public void RemoveLocation(string name)
        {
            _availableLocations.RemoveAll(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}

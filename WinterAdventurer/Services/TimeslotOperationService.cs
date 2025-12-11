// <copyright file="TimeslotOperationService.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Data;
using WinterAdventurer.Library.Models;
using WinterAdventurer.Library.Services;
using WinterAdventurer.Models;
using DataTimeSlot = WinterAdventurer.Data.TimeSlot;
using LibraryTimeSlot = WinterAdventurer.Library.Models.TimeSlot;

namespace WinterAdventurer.Services
{
    /// <summary>
    /// Default implementation of ITimeslotOperationService.
    /// </summary>
    public class TimeslotOperationService : ITimeslotOperationService
    {
        public List<TimeSlotViewModel> PopulateTimeslotsFromPeriods(
            List<Period> periods,
            List<TimeSlotViewModel> existingTimeslots)
        {
            var result = new List<TimeSlotViewModel>();

            // Create a mapping of existing period labels to preserve IDs
            var existingPeriods = existingTimeslots
                .Where(t => t.IsPeriod)
                .ToDictionary(t => t.Label, t => t);

            // Keep user-added timeslots
            var userSlots = existingTimeslots.Where(t => !t.IsPeriod).ToList();

            // Add or create period timeslots
            foreach (var period in periods)
            {
                if (existingPeriods.TryGetValue(period.DisplayName, out var existingSlot))
                {
                    // Reuse existing slot to preserve @key identity
                    result.Add(existingSlot);
                }
                else
                {
                    // Create new slot for new periods
                    result.Add(new TimeSlotViewModel
                    {
                        Label = period.DisplayName,
                        IsPeriod = true,
                        StartTime = null,
                        EndTime = null,
                    });
                }
            }

            // Re-add user slots
            result.AddRange(userSlots);

            // Sort and validate
            SortTimeslots(result);

            return result;
        }

        public void SortTimeslots(List<TimeSlotViewModel> timeslots)
        {
            // Sort in-place by start time
            timeslots.Sort((a, b) =>
            {
                var aTime = a.StartTime ?? TimeSpan.MaxValue;
                var bTime = b.StartTime ?? TimeSpan.MaxValue;
                return aTime.CompareTo(bTime);
            });
        }

        public void ValidateTimeslots(
            List<TimeSlotViewModel> timeslots,
            ITimeslotValidationService validator,
            out bool hasOverlapping,
            out bool hasUnconfigured)
        {
            var timeslotDtos = timeslots.Select(t => new TimeSlotDto
            {
                Id = t.Id,
                Label = t.Label,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                IsPeriod = t.IsPeriod,
            });

            var validationResult = validator.ValidateTimeslots(timeslotDtos);
            hasOverlapping = validationResult.HasOverlappingTimeslots;
            hasUnconfigured = validationResult.HasUnconfiguredTimeslots;
        }

        public List<LibraryTimeSlot> ConvertToLibraryTimeSlots(List<TimeSlotViewModel> viewModels)
        {
            return viewModels.Select(t => new LibraryTimeSlot
            {
                Id = t.Id,
                Label = t.Label,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                IsPeriod = t.IsPeriod,
            }).ToList();
        }

        public async Task<TimeslotOperationResult> SaveTimeslotsAsync(
            List<TimeSlotViewModel> timeslots,
            ILocationService locationService,
            ITimeslotValidationService validator)
        {
            var result = new TimeslotOperationResult { Success = true };

            try
            {
                // Sort and validate before saving
                SortTimeslots(timeslots);
                ValidateTimeslots(timeslots, validator, out var hasOverlap, out var hasUnconfigured);

                result.HasOverlappingTimeslots = hasOverlap;
                result.HasUnconfiguredTimeslots = hasUnconfigured;

                // Convert and save
                var dbTimeSlots = timeslots.Select(t => new DataTimeSlot
                {
                    Id = t.Id,
                    Label = t.Label,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    IsPeriod = t.IsPeriod,
                }).ToList();

                await locationService.SaveAllTimeSlotsAsync(dbTimeSlots);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error saving timeslots: {ex.Message}";
            }

            return result;
        }
    }
}

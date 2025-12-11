// <copyright file="TestDataFactory.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.AspNetCore.Components.Forms;
using WinterAdventurer.Data;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Test.Mocks;

/// <summary>
/// Factory class for creating test data objects with sensible defaults.
/// Provides consistent test data creation across all component tests.
/// </summary>
public static class TestDataFactory
{

    /// <summary>
    /// Creates a test workshop with customizable properties.
    /// </summary>
    /// <returns></returns>
    public static Workshop CreateWorkshop(
        string name = "Test Workshop",
        string location = "Test Location",
        string periodSheetName = "MorningFirstPeriod",
        string leader = "Test Leader",
        int startDay = 1,
        int endDay = 4,
        bool isMini = false,
        int maxParticipants = 15,
        int minAge = 0,
        List<WorkshopSelection>? selections = null,
        List<LocationTag>? tags = null)
    {
        return new Workshop
        {
            Name = name,
            Location = location,
            Period = new Period(periodSheetName),
            Leader = leader,
            Duration = new WorkshopDuration(startDay, endDay),
            IsMini = isMini,
            MaxParticipants = maxParticipants,
            MinAge = minAge,
            Selections = selections ?? new List<WorkshopSelection>(),
            Tags = tags,
        };
    }

    /// <summary>
    /// Creates multiple test workshops with unique names.
    /// </summary>
    /// <returns></returns>
    public static List<Workshop> CreateWorkshops(int count, string periodSheetName = "MorningFirstPeriod")
    {
        var workshops = new List<Workshop>();
        for (int i = 1; i <= count; i++)
        {
            workshops.Add(CreateWorkshop(
                name: $"Workshop {i}",
                location: $"Location {i}",
                periodSheetName: periodSheetName,
                leader: $"Leader {i}"));
        }

        return workshops;
    }

    /// <summary>
    /// Creates a test workshop selection (participant registration).
    /// </summary>
    /// <returns></returns>
    public static WorkshopSelection CreateSelection(
        string classSelectionId = "SEL001",
        string workshopName = "Test Workshop",
        string firstName = "John",
        string lastName = "Doe",
        int choiceNumber = 1,
        int startDay = 1,
        int endDay = 4,
        int registrationId = 1)
    {
        return new WorkshopSelection
        {
            ClassSelectionId = classSelectionId,
            WorkshopName = workshopName,
            FirstName = firstName,
            LastName = lastName,
            FullName = $"{firstName} {lastName}",
            ChoiceNumber = choiceNumber,
            Duration = new WorkshopDuration(startDay, endDay),
            RegistrationId = registrationId,
        };
    }

    /// <summary>
    /// Creates multiple test selections for a workshop.
    /// </summary>
    /// <returns></returns>
    public static List<WorkshopSelection> CreateSelections(
        int count,
        string workshopName = "Test Workshop",
        int choiceNumber = 1)
    {
        var selections = new List<WorkshopSelection>();
        for (int i = 1; i <= count; i++)
        {
            selections.Add(CreateSelection(
                classSelectionId: $"SEL{i:D3}",
                workshopName: workshopName,
                firstName: $"First{i}",
                lastName: $"Last{i}",
                choiceNumber: choiceNumber,
                registrationId: i));
        }

        return selections;
    }

    /// <summary>
    /// Creates a test time slot (Library model).
    /// </summary>
    /// <returns></returns>
    public static Library.Models.TimeSlot CreateTimeslot(
        string? id = null,
        string label = "Test Period",
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        bool isPeriod = true)
    {
        return new Library.Models.TimeSlot
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Label = label,
            StartTime = startTime ?? new TimeSpan(9, 0, 0),
            EndTime = endTime ?? new TimeSpan(10, 30, 0),
            IsPeriod = isPeriod,
        };
    }

    /// <summary>
    /// Creates a standard set of period timeslots for a 4-day event.
    /// </summary>
    /// <returns></returns>
    public static List<Library.Models.TimeSlot> CreateStandardTimeslots()
    {
        return new List<Library.Models.TimeSlot>
        {
            CreateTimeslot(label: "MorningFirstPeriod", startTime: new TimeSpan(9, 0, 0), endTime: new TimeSpan(10, 30, 0), isPeriod: true),
            CreateTimeslot(label: "MorningSecondPeriod", startTime: new TimeSpan(11, 0, 0), endTime: new TimeSpan(12, 30, 0), isPeriod: true),
            CreateTimeslot(label: "Lunch", startTime: new TimeSpan(12, 30, 0), endTime: new TimeSpan(13, 30, 0), isPeriod: false),
            CreateTimeslot(label: "AfternoonFirstPeriod", startTime: new TimeSpan(13, 30, 0), endTime: new TimeSpan(15, 0, 0), isPeriod: true),
            CreateTimeslot(label: "AfternoonSecondPeriod", startTime: new TimeSpan(15, 30, 0), endTime: new TimeSpan(17, 0, 0), isPeriod: true),
        };
    }

    /// <summary>
    /// Creates timeslots with overlapping times for validation testing.
    /// </summary>
    /// <returns></returns>
    public static List<Library.Models.TimeSlot> CreateOverlappingTimeslots()
    {
        return new List<Library.Models.TimeSlot>
        {
            CreateTimeslot(label: "Period 1", startTime: new TimeSpan(9, 0, 0), endTime: new TimeSpan(10, 30, 0), isPeriod: true),
            CreateTimeslot(label: "Period 2", startTime: new TimeSpan(10, 0, 0), endTime: new TimeSpan(11, 30, 0), isPeriod: true), // Overlaps with Period 1
        };
    }

    /// <summary>
    /// Creates timeslots with missing times for validation testing.
    /// </summary>
    /// <returns></returns>
    public static List<Library.Models.TimeSlot> CreateUnconfiguredTimeslots()
    {
        return new List<Library.Models.TimeSlot>
        {
            CreateTimeslot(label: "Period 1", startTime: new TimeSpan(9, 0, 0), endTime: new TimeSpan(10, 30, 0), isPeriod: true),
            CreateTimeslot(label: "Period 2", startTime: null, endTime: null, isPeriod: true), // Unconfigured
        };
    }

    /// <summary>
    /// Creates a test location entity.
    /// </summary>
    /// <returns></returns>
    public static Location CreateLocation(
        int id = 1,
        string name = "Test Location",
        DateTime? createdAt = null,
        List<Tag>? tags = null)
    {
        return new Location
        {
            Id = id,
            Name = name,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Tags = tags ?? new List<Tag>(),
        };
    }

    /// <summary>
    /// Creates multiple test locations.
    /// </summary>
    /// <returns></returns>
    public static List<Location> CreateLocations(int count)
    {
        var locations = new List<Location>();
        for (int i = 1; i <= count; i++)
        {
            locations.Add(CreateLocation(id: i, name: $"Location {i}"));
        }

        return locations;
    }

    /// <summary>
    /// Creates a test location tag.
    /// </summary>
    /// <returns></returns>
    public static LocationTag CreateLocationTag(string name = "Test Tag")
    {
        return new LocationTag { Name = name };
    }

    /// <summary>
    /// Creates a test tag entity (database).
    /// </summary>
    /// <returns></returns>
    public static Tag CreateTag(
        int id = 1,
        string name = "Test Tag",
        DateTime? createdAt = null)
    {
        return new Tag
        {
            Id = id,
            Name = name,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a mock IBrowserFile for file upload testing.
    /// </summary>
    /// <returns></returns>
    public static IBrowserFile CreateMockBrowserFile(
        string name = "test.xlsx",
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        long size = 1024,
        byte[] ? content = null)
    {
        return new MockBrowserFile(name, contentType, size, content);
    }

    /// <summary>
    /// Creates a mock Excel file with sample byte content.
    /// </summary>
    /// <returns></returns>
    public static IBrowserFile CreateMockExcelFile(string name = "test.xlsx")
    {
        // Simple byte array representing a minimal file
        var content = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // ZIP header (Excel files are ZIP archives)
        return CreateMockBrowserFile(name, content: content);
    }

    /// <summary>
    /// Creates a test period.
    /// </summary>
    /// <returns></returns>
    public static Period CreatePeriod(string sheetName = "MorningFirstPeriod")
    {
        return new Period(sheetName);
    }

    /// <summary>
    /// Creates a test workshop duration.
    /// </summary>
    /// <returns></returns>
    public static WorkshopDuration CreateDuration(int startDay = 1, int endDay = 4)
    {
        return new WorkshopDuration(startDay, endDay);
    }
}

/// <summary>
/// Mock implementation of IBrowserFile for testing file upload scenarios.
/// </summary>
public class MockBrowserFile : IBrowserFile
{
    private readonly byte[] _content;

    public MockBrowserFile(string name, string contentType, long size, byte[] ? content = null)
    {
        Name = name;
        ContentType = contentType;
        Size = size;
        _content = content ?? new byte[size];
        LastModified = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public string Name { get; }
    /// <inheritdoc/>
    public DateTimeOffset LastModified { get; }
    /// <inheritdoc/>
    public long Size { get; }
    /// <inheritdoc/>
    public string ContentType { get; }

    /// <inheritdoc/>
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        if (Size > maxAllowedSize)
        {
            throw new IOException($"The file size {Size} exceeds the maximum allowed size {maxAllowedSize}.");
        }

        return new MemoryStream(_content);
    }
}

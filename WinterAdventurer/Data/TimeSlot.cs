using System.ComponentModel.DataAnnotations;

namespace WinterAdventurer.Data;

/// <summary>
/// Represents a timeslot in the schedule (workshop periods, lunch, breaks, etc.)
/// </summary>
public class TimeSlot
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// True if this is a period from the Excel file, false if user-added (lunch, break, etc.)
    /// </summary>
    public bool IsPeriod { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace WinterAdventurer.Data;

/// <summary>
/// Maps workshop names to their assigned locations for persistence across sessions
/// </summary>
public class WorkshopLocationMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string WorkshopName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string LocationName { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

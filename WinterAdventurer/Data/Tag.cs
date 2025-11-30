using System.ComponentModel.DataAnnotations;

namespace WinterAdventurer.Data;

/// <summary>
/// Represents a tag that can be applied to locations (e.g., "Downstairs", "Accessible")
/// </summary>
public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional color for UI display (e.g., "#FF5722" for orange)
    /// </summary>
    [MaxLength(20)]
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for many-to-many
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}

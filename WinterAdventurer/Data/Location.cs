using System.ComponentModel.DataAnnotations;

namespace WinterAdventurer.Data;

/// <summary>
/// Represents a location where workshops can be held
/// </summary>
public class Location
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for many-to-many relationship with Tags
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

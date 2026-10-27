using System.ComponentModel.DataAnnotations;

namespace IceCreamOrderingSystem.Models;

public class Review
{
    public int Id { get; set; }

    public int IceCreamId { get; set; }

    public IceCream? IceCream { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

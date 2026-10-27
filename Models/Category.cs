using System.ComponentModel.DataAnnotations;

namespace IceCreamOrderingSystem.Models;

public class Category
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<IceCream> IceCreams { get; set; } = new List<IceCream>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceCreamOrderingSystem.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required, StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Column(TypeName = "numeric(10,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(30)]
    public string PaymentMethod { get; set; } = "COD";

    [StringLength(30)]
    public string PaymentStatus { get; set; } = "Pending";

    [StringLength(30)]
    public string OrderStatus { get; set; } = "Pending";

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();

    public Payment? Payment { get; set; }
}

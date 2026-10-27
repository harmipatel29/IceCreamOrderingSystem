using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceCreamOrderingSystem.Models;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    [Required, StringLength(30)]
    public string PaymentMethod { get; set; } = "COD";

    [Required, StringLength(30)]
    public string Status { get; set; } = "Pending";

    [StringLength(200)]
    public string? TransactionId { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }
}

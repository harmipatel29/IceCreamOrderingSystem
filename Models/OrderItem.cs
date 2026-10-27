using System.ComponentModel.DataAnnotations.Schema;

namespace IceCreamOrderingSystem.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public int IceCreamId { get; set; }

    public IceCream? IceCream { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal Subtotal => Quantity * UnitPrice;
}

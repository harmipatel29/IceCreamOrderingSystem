
namespace IceCreamOrderingSystem.Models
{
    public class CartItemViewModel
    {
        public int IceCreamId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal Subtotal => Price * Quantity;
    }
}

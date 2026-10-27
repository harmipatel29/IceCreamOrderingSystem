using System.ComponentModel.DataAnnotations;

namespace IceCreamOrderingSystem.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your phone number.")]
        [RegularExpression(@"^[6-9]\d{9}$",
            ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your delivery address.")]
        [StringLength(500)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required]
        public string PaymentMethod { get; set; } = "COD";

        [StringLength(100)]
        public string? TransactionId { get; set; }

        public List<CartItemViewModel> CartItems { get; set; } = new();

        public decimal TotalAmount => CartItems.Sum(item => item.Price * item.Quantity);
    }
}
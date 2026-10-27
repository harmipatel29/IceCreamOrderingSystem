using System.Text.Json;
using System.Security.Claims;
using IceCreamOrderingSystem.Data;
using IceCreamOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IceCreamOrderingSystem.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string CartSessionKey = "FrostyDelightCart";

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItemViewModel> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrWhiteSpace(json))
                return new List<CartItemViewModel>();

            try
            {
                return JsonSerializer.Deserialize<List<CartItemViewModel>>(json)
                       ?? new List<CartItemViewModel>();
            }
            catch (JsonException)
            {
                return new List<CartItemViewModel>();
            }
        }

        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(
                CartSessionKey,
                JsonSerializer.Serialize(cart));
        }

        public IActionResult Index()
        {
            var cart = GetCart();

            ViewBag.Total = cart.Sum(item => item.Subtotal);

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            if (quantity < 1)
                quantity = 1;

            var iceCream = await _context.IceCreams
                .FirstOrDefaultAsync(i => i.Id == id);

            if (iceCream == null ||
                !iceCream.IsAvailable ||
                iceCream.Stock <= 0)
            {
                TempData["CartError"] =
                    "This ice cream is currently unavailable.";

                return RedirectToAction("Shop", "Home");
            }

            var cart = GetCart();

            var existing = cart.FirstOrDefault(item => item.IceCreamId == id);
            var requestedQuantity = (existing?.Quantity ?? 0) + quantity;

            if (requestedQuantity > iceCream.Stock)
            {
                TempData["CartError"] =
                    $"Only {iceCream.Stock} unit(s) of {iceCream.Name} are available.";

                return RedirectToAction("Shop", "Home");
            }

            if (existing != null)
            {
                existing.Quantity = requestedQuantity;
                existing.Price = iceCream.Price;
                existing.Name = iceCream.Name;
                existing.ImageUrl = iceCream.ImageUrl;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    IceCreamId = iceCream.Id,
                    Name = iceCream.Name,
                    ImageUrl = iceCream.ImageUrl,
                    Price = iceCream.Price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);

            TempData["CartSuccess"] =
                $"{iceCream.Name} added to your cart!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.IceCreamId == id);

            if (item == null)
                return RedirectToAction(nameof(Index));

            if (quantity <= 0)
            {
                cart.Remove(item);
                SaveCart(cart);

                return RedirectToAction(nameof(Index));
            }

            var iceCream = await _context.IceCreams
                .FirstOrDefaultAsync(i => i.Id == id);

            if (iceCream == null ||
                !iceCream.IsAvailable ||
                quantity > iceCream.Stock)
            {
                TempData["CartError"] =
                    "Requested quantity is not available in stock.";

                return RedirectToAction(nameof(Index));
            }

            item.Quantity = quantity;
            item.Price = iceCream.Price;
            item.Name = iceCream.Name;
            item.ImageUrl = iceCream.ImageUrl;

            SaveCart(cart);

            TempData["CartSuccess"] = "Cart updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = GetCart();

            cart.RemoveAll(item => item.IceCreamId == id);

            SaveCart(cart);

            TempData["CartSuccess"] = "Item removed from cart.";

            return RedirectToAction(nameof(Index));
        }

        // CHECKOUT - GET
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                TempData["CartError"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);

            var model = new CheckoutViewModel
            {
                CustomerName = user?.FullName ?? string.Empty,
                Phone = user?.PhoneNumber ?? string.Empty,
                PaymentMethod = "COD",
                CartItems = cart
            };

            return View(model);
        }

        // CHECKOUT - POST
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                TempData["CartError"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            model.CartItems = cart;

            // Only COD is currently supported.
            if (!string.Equals(
                    model.PaymentMethod,
                    "COD",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethod),
                    "Please select Cash on Delivery.");

                model.PaymentMethod = "COD";
            }

            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Verify current database prices and stock.
            var productIds = cart.Select(item => item.IceCreamId)
                .Distinct()
                .ToList();

            var products = await _context.IceCreams
                .Where(i => productIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            foreach (var item in cart)
            {
                if (!products.TryGetValue(item.IceCreamId, out var product) ||
                    !product.IsAvailable)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"{item.Name} is no longer available.");

                    continue;
                }

                if (item.Quantity <= 0 || item.Quantity > product.Stock)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Only {product.Stock} unit(s) of {product.Name} are currently available.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    UserId = userId,
                    CustomerName = model.CustomerName.Trim(),
                    Phone = model.Phone.Trim(),
                    DeliveryAddress = model.DeliveryAddress.Trim(),
                    PaymentMethod = "COD",
                    PaymentStatus = "Pending",
                    OrderStatus = "Pending",
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = 0
                };

                foreach (var item in cart)
                {
                    var product = products[item.IceCreamId];

                    // Use database price, not the session price.
                    order.OrderItems.Add(new OrderItem
                    {
                        IceCreamId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    });

                    order.TotalAmount += product.Price * item.Quantity;

                    product.Stock -= item.Quantity;

                    if (product.Stock == 0)
                        product.IsAvailable = false;
                }

                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                var payment = new Payment
                {
                    OrderId = order.Id,
                    PaymentMethod = "COD",
                    Status = "Pending",
                    Amount = order.TotalAmount,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Clear the cart only after successful order creation.
                HttpContext.Session.Remove(CartSessionKey);

                TempData["OrderSuccess"] =
                    "Your order has been placed successfully!";

                return RedirectToAction(
                    nameof(OrderConfirmation),
                    new { id = order.Id });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "We could not place your order. Please try again.");

                return View(model);
            }
        }

        // ORDER CONFIRMATION
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.IceCream)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
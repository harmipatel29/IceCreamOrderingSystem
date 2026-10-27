
using IceCreamOrderingSystem.Data;
using IceCreamOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IceCreamOrderingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalIceCreams = await _context.IceCreams.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalCustomers = await _context.Users
                .CountAsync(u => u.Role == "Customer");

            ViewBag.PendingOrders = await _context.Orders
                .CountAsync(o => o.OrderStatus == "Pending");

            ViewBag.RecentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> IceCreams()
        {
            var iceCreams = await _context.IceCreams
                .Include(i => i.Category)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(iceCreams);
        }

        public async Task<IActionResult> CreateIceCream()
        {
            await LoadCategories();
            return View(new IceCream());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIceCream(IceCream iceCream)
        {
            if (ModelState.IsValid)
            {
                iceCream.CreatedAt = DateTime.UtcNow;
                _context.IceCreams.Add(iceCream);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Ice cream added successfully!";
                return RedirectToAction(nameof(IceCreams));
            }

            await LoadCategories();
            return View(iceCream);
        }

        public async Task<IActionResult> EditIceCream(int id)
        {
            var iceCream = await _context.IceCreams.FindAsync(id);

            if (iceCream == null)
                return NotFound();

            await LoadCategories();
            return View(iceCream);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditIceCream(int id, IceCream iceCream)
        {
            if (id != iceCream.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadCategories();
                return View(iceCream);
            }

            var existing = await _context.IceCreams.FindAsync(id);

            if (existing == null)
                return NotFound();

            existing.Name = iceCream.Name;
            existing.Description = iceCream.Description;
            existing.Price = iceCream.Price;
            existing.ImageUrl = iceCream.ImageUrl;
            existing.Stock = iceCream.Stock;
            existing.IsAvailable = iceCream.IsAvailable;
            existing.CategoryId = iceCream.CategoryId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Ice cream updated successfully!";
            return RedirectToAction(nameof(IceCreams));
        }

        public async Task<IActionResult> DeleteIceCream(int id)
        {
            var iceCream = await _context.IceCreams
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (iceCream == null)
                return NotFound();

            return View(iceCream);
        }

        [HttpPost, ActionName("DeleteIceCream")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIceCreamConfirmed(int id)
        {
            var iceCream = await _context.IceCreams.FindAsync(id);

            if (iceCream == null)
                return NotFound();

            var hasOrders = await _context.OrderItems
                .AnyAsync(oi => oi.IceCreamId == id);

            if (hasOrders)
            {
                TempData["Error"] =
                    "This ice cream is linked to existing orders. Mark it unavailable instead of deleting it.";

                return RedirectToAction(nameof(IceCreams));
            }

            _context.IceCreams.Remove(iceCream);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ice cream deleted successfully!";
            return RedirectToAction(nameof(IceCreams));
        }

        private async Task LoadCategories()
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}

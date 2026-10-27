
using IceCreamOrderingSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IceCreamOrderingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var iceCreams = await _context.IceCreams
                .Include(i => i.Category)
                .Where(i => i.IsAvailable && i.Stock > 0)
                .OrderByDescending(i => i.CreatedAt)
                .Take(8)
                .ToListAsync();

            return View(iceCreams);
        }

        public async Task<IActionResult> Shop(int? categoryId, string? search)
        {
            var query = _context.IceCreams
                .Include(i => i.Category)
                .Where(i => i.IsAvailable && i.Stock > 0)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.Name.Contains(search));
            }

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.SelectedCategory = categoryId;
            ViewBag.Search = search;

            var products = await query
                .OrderBy(i => i.Name)
                .ToListAsync();

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}

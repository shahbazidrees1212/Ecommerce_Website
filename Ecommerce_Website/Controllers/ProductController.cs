using Microsoft.AspNetCore.Mvc;
using Ecommerce_Website.Data;
using Ecommerce_Website.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_Website.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // SHOW ALL PRODUCTS
        public async Task<IActionResult> Index(int? categoryId, string search, int page = 1)
        {
            int pageSize = 8;

            var query = _context.Products.AsQueryable();

            // ✅ CATEGORY FILTER
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // ✅ SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));
            }

            int totalProducts = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;

            return View(products);
        }

        // PRODUCT DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
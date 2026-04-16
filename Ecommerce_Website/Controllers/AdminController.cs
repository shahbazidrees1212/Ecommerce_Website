using Ecommerce_Website.Data;
using Ecommerce_Website.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_Website.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        public AdminController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }
         // CATEGORY LIST
    public IActionResult Category(int page = 1)
    {
        int pageSize = 5;

        var totalItems = _context.Categories.Count();

        var categories = _context.Categories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(categories); // Views/Admin/Category.cshtml
    }
        public async Task<IActionResult> Index()
        {
            // 🔹 Counts
            ViewBag.ProductsCount = await _context.Products.CountAsync();
            ViewBag.CategoriesCount = await _context.Categories.CountAsync();
            ViewBag.OrdersCount = await _context.Orders.CountAsync();
            ViewBag.UsersCount = await _context.Users.CountAsync();

            // 💰 Revenue
            ViewBag.TotalRevenue = await _context.Orders
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // 📦 Recent Orders
            ViewBag.RecentOrders = await _context.Orders
                .OrderByDescending(o => o.Id)
                .Take(5)
                .ToListAsync();

            // ⚠️ Low Stock
            ViewBag.LowStock = await _context.Products
                .Where(p => p.Stock <= 5)
                .ToListAsync();

            return View();
        }
        // PRODUCT LIST
        public async Task<IActionResult> Products(int page = 1)
        {
            int pageSize = 5;

            var products = await _context.Products
                .Include(p => p.Category)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalProducts = await _context.Products.CountAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            return View(products);
        }

        // CREATE VIEW
        public IActionResult CreateProduct()
        {
            ViewBag.CategoryId =
                new SelectList(_context.Categories, "Id", "Name");

            return View();
        }

        // CREATE PRODUCT
        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (product.ImageFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "images/products");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() +
                                  Path.GetExtension(product.ImageFile.FileName);

                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.ImageFile.CopyToAsync(stream);
                }

                product.ImageUrl = "/images/products/" + fileName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Products");
        }

        // EDIT VIEW
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            ViewBag.CategoryId =
                new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        // EDIT PRODUCT
        [HttpPost]
        public async Task<IActionResult> EditProduct(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Products");
        }

        // DELETE PRODUCT
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

            return View(product);
        }

        [HttpPost, ActionName("DeleteProduct")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Products");
        }

        //public IActionResult Categories()
        //{
        //    return RedirectToAction("Index", "Category");
        //}
        //Order
        public IActionResult Orders(int page = 1)
        {
            int pageSize = 10;

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate);

            int totalOrders = query.Count();

            var orders = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalOrders == 0 ? 1 : (int)Math.Ceiling((double)totalOrders / pageSize);

            return View(orders);
        }
        //Order Status
        [HttpPost]
        public IActionResult UpdateOrderStatus(int orderId, string status)
        {
            var order = _context.Orders.Find(orderId);

            if (order != null)
            {
                order.Status = status;
                _context.SaveChanges();
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        //OrderDeatils
        public IActionResult OrderDetails(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        public IActionResult Users(string search, int page = 1)
        {
            int pageSize = 5;

            // 🔍 Step 1: Get users query
            var query = _userManager.Users.AsQueryable();

            // 🔍 Step 2: Apply search (Email / Phone)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.Email.Contains(search) ||
                    u.PhoneNumber.Contains(search));
            }

            // 📊 Step 3: Count total users
            int totalUsers = query.Count();

            // 📄 Step 4: Apply pagination
            var users = query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 📦 Step 5: Pass data to view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.Search = search;

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBlockUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // 🔥 Toggle logic
            user.IsBlocked = !user.IsBlocked;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest("Failed to update user");
            }

            return RedirectToAction("Users");
        }
    }
}
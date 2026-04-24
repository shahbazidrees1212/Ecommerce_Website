using Ecommerce_Website.Data;
using Ecommerce_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Ecommerce_Website.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        public OrderController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ============================
        // CHECKOUT PAGE
        // ============================
        public IActionResult Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToList();

            return View(cartItems);
        }

        // ============================
        // CREATE ORDER
        // ============================
        private void CreateOrder(string userId)
        {
            var cartItems = _context.Carts
                .Where(c => c.UserId == userId)
                .ToList();

            if (!cartItems.Any())
                return;

            Order order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Paid",
                TotalAmount = 0
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            decimal total = 0;

            foreach (var item in cartItems)
            {
                var product = _context.Products.Find(item.ProductId);

                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price
                });

                total += product.Price * item.Quantity;
            }

            order.TotalAmount = total;

            _context.Carts.RemoveRange(cartItems);
            _context.SaveChanges();

        }
        // ============================
        // DEMO PAYMENT
        // ============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(string cardNumber, string expiry, string cvv, string cardName)
        {
            Console.WriteLine("🔥 HIT");

            if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(cvv))
            {
                TempData["Error"] = "Invalid payment!";
                return RedirectToAction("Checkout");
            }

            return RedirectToAction("StripeSuccess");
        }
        // ============================
        // STRIPE SUCCESS
        // ============================
        public IActionResult StripeSuccess()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            CreateOrder(userId);

            var order = _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Id)
                .FirstOrDefault();

            return RedirectToAction("Success", new
            {
                orderId = order.Id,
                amount = order.TotalAmount
            });
        }

        // ============================
        // PAYPAL SUCCESS
        // ============================
        public async Task<IActionResult> PayPalSuccess(string token)
        {
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];
            var baseUrl = "https://api-m.sandbox.paypal.com";

            using var client = new HttpClient();

            // 🔹 GET TOKEN
            var auth = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{secret}")
            );

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);

            var tokenResponse = await client.PostAsync(
                $"{baseUrl}/v1/oauth2/token",
                new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string,string>("grant_type","client_credentials")
                })
            );

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenData = JsonConvert.DeserializeObject(tokenJson);
            string accessToken = tokenData.access_token;

            // 🔹 CAPTURE FIX
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var captureResponse = await client.PostAsync(
                $"{baseUrl}/v2/checkout/orders/{token}/capture",
                new StringContent("", Encoding.UTF8, "application/json") // ✅ FIX
            );

            var captureJson = await captureResponse.Content.ReadAsStringAsync();

            if (!captureResponse.IsSuccessStatusCode)
            {
                return Content("Capture Error: " + captureJson);
            }

            return RedirectToAction("Success");
        }
        // ============================
        // MY ORDERS
        // ============================
        public IActionResult MyOrders(int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int pageSize = 5;

            var query = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate);

            int totalOrders = query.Count();

            var orders = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            return View(orders);
        }

        // ============================
        // ORDER DETAILS
        // ============================
        public IActionResult OrderDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            return View(order);
        }


        // ============================
        // CANCEL ORDER
        // ============================
        public IActionResult CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = _context.Orders
                .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            // ❌ Do not allow cancel after shipping/delivery
            if (order.Status == "Shipped" || order.Status == "Delivered")
            {
                TempData["Error"] = "Order cannot be cancelled after shipping!";
                return RedirectToAction("MyOrders");
            }

            // ✅ Allow cancel for Pending & Paid
            if (order.Status == "Pending" || order.Status == "Paid")
            {
                order.Status = "Cancelled";
                _context.SaveChanges();

                TempData["Success"] = "Order cancelled successfully!";
            }

            return RedirectToAction("MyOrders");
        }
        public IActionResult Success(int orderId, decimal amount)
        {
            ViewBag.OrderId = orderId;
            ViewBag.Amount = amount;
            ViewBag.Message = "Your payment has been successfully processed.";

            return View();
        }
    }
}
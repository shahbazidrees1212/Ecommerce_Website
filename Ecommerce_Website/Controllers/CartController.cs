using Ecommerce_Website.Data;
using Ecommerce_Website.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Ecommerce_Website.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        public CartController(IConfiguration config,ApplicationDbContext context)
        {
            _context = context;
            _config = config;
        }

        // ============================
        // VIEW CART
        // ============================
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToList();

            return View(cartItems);
        }

        // ============================
        // ADD TO CART
        // ============================
        public IActionResult AddToCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingItem = _context.Carts
                .FirstOrDefault(c => c.ProductId == productId && c.UserId == userId);

            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    ProductId = productId,
                    Quantity = 1,
                    UserId = userId
                });
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        
        // ============================
        // STRIPE CHECKOUT
        // ============================
        public IActionResult StripeCheckout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToList();

            StripeConfiguration.ApiKey = "sk_test_51SSuIiJebi6OKU3blz8qOwfLNA5S86ch9e09nTUnuIEbOkLmveKcsoO36cyIHhKN7l0evHlLOYDl3MN2vHYC3ERU00R8TrJTv2"; // 🔴 your key

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = cartItems.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Product.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name
                        },
                    },
                    Quantity = item.Quantity,
                }).ToList(),

                Mode = "payment",

                // ✅ IMPORTANT FIX
                SuccessUrl = Url.Action("StripeSuccess", "Order", null, Request.Scheme),
                CancelUrl = Url.Action("Checkout", "Order", null, Request.Scheme),
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }

        // ============================
        // PAYPAL CHECKOUT
        // ============================
        public async Task<IActionResult> PayPalCheckout()
        {
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];
            var baseUrl = _config["PayPal:BaseUrl"];

            // 🔥 SAFETY CHECK (fix your error)
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "https://api-m.sandbox.paypal.com";
            }

            using var client = new HttpClient();

            // STEP 1: GET TOKEN
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

            if (!tokenResponse.IsSuccessStatusCode)
            {
                return Content("Token Error: " + tokenJson);
            }

            dynamic tokenData = JsonConvert.DeserializeObject(tokenJson);
            string accessToken = tokenData.access_token;

            // STEP 2: CREATE ORDER
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var order = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                new {
                    amount = new {
                        currency_code = "USD",
                        value = "10.00"
                    }
                }
            },
                application_context = new
                {
                    return_url = Url.Action("PayPalSuccess", "Order", null, Request.Scheme),
                    cancel_url = Url.Action("Checkout", "Order", null, Request.Scheme)
                }
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(order),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                $"{baseUrl}/v2/checkout/orders",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Content("Order Error: " + json);
            }

            dynamic data = JsonConvert.DeserializeObject(json);

            foreach (var link in data.links)
            {
                if (link.rel == "approve")
                {
                    return Redirect((string)link.href);
                }
            }

            return Content("Approval URL not found");
        }
        // ============================
        // REMOVE ITEM
        // ============================
        public IActionResult Remove(int id)
        {
            var cartItem = _context.Carts.Find(id);

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
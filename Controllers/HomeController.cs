using Ecommerce_Website.Data;
using Ecommerce_Website.Models;
using Ecommerce_Website.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;

namespace Ecommerce_Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailService;

        public HomeController(IEmailSender emailService,
            ApplicationDbContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            // 🔹 Categories
            var categories = await _context.Categories
      .Include(c => c.Products) // 🔥 IMPORTANT
      .OrderByDescending(c => c.Id)
      .Take(8)
      .ToListAsync();
            // 🔹 Products Query
            var productsQuery = _context.Products.AsQueryable();

            // ❌ DO NOT FILTER STOCK (important fix)
            // productsQuery = productsQuery.Where(p => p.Stock > 0);

            // 🔍 Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                productsQuery = productsQuery.Where(p =>
                    EF.Functions.Like(p.Name, $"%{search}%") ||
                    EF.Functions.Like(p.Description, $"%{search}%")
                );
            }

            // 🔹 Get Products
            var products = await productsQuery
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToListAsync();

            // 🔹 ViewModel
            var model = new HomeViewModel
            {
                Categories = categories,
                Products = products
            };

            ViewBag.Search = search;

            return View(model);
        }
        
            [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 🔥 Step 1: Find user first
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View(model);
            }

            // ❌ Step 2: Check if user is blocked
            if (user.IsBlocked)
            {
                ModelState.AddModelError("", "Your account has been blocked by admin.");
                return View(model);
            }

            // ✅ Step 3: Proceed with login
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Admin login
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                // Normal user
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid login attempt");
            return View(model);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email already registered");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }

        //Change Password
        // GET
        public IActionResult ChangePassword()
        {
            var email = User.Identity.Name;

            return View(new ChangePasswordViewModel
            {
                Email = email
            });
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword
            );

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password updated successfully!";
                return RedirectToAction("ChangePassword");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
        //Forogott password
        // GET
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                Console.WriteLine("📩 Request for: " + model.Email);

                var user = await _userManager.FindByEmailAsync(model.Email);

                Console.WriteLine(user == null ? "❌ User NOT found" : "✅ User found");

                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                    // ✅ ENCODE TOKEN (IMPORTANT)
                    var encodedToken = WebEncoders.Base64UrlEncode(
                        Encoding.UTF8.GetBytes(token)
                    );

                    var callbackUrl = Url.Action(
                        "ResetPassword",
                        "Home",
                        new
                        {
                            token = encodedToken,
                            email = model.Email
                        },
                        protocol: HttpContext.Request.Scheme
                    );

                    Console.WriteLine("🔗 Link: " + callbackUrl);

                    var emailBody = $@"
            <div style='font-family:Arial; padding:20px'>
                <h2>Password Reset</h2>
                <p>Click below button:</p>

                <a href='{callbackUrl}' 
                   style='padding:10px 20px;
                          background:#facc15;
                          text-decoration:none;
                          border-radius:5px;
                          color:black'>
                   Reset Password
                </a>

                <p>If not requested, ignore.</p>
            </div>";

                    await _emailService.SendEmailAsync(
                        model.Email,
                        "Reset Your Password",
                        emailBody
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Controller Error: " + ex.ToString());
            }

            TempData["Success"] = "If account exists, email sent.";
            return RedirectToAction("ForgotPassword");
        }

        //Reset Password
        // GET
        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(model);
            }

            // 🔥 FIX: Decode token
            var decodedToken = System.Text.Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(model.Token)
            );

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                model.Password
            );

            if (result.Succeeded)
            {
                TempData["Success"] = "Password reset successfully!";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
    }
}
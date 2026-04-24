using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Ecommerce_Website.Models;
using Ecommerce_Website.ViewModels;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public AccountController(UserManager<ApplicationUser> userManager,
                             IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
    }

    // GET Profile
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return RedirectToAction("Login", "Home");
        }

        var model = new ProfileViewModel
        {
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            ProfilePicture = user.ProfilePicture
        };

        return View(model);
    }

    // POST Profile Update
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        user.Name = model.Name;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;

        // Upload Image
        if (model.ProfileImage != null)
        {
            string folder = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);

            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfileImage.CopyToAsync(stream);
            }

            user.ProfilePicture = "/uploads/" + fileName;
        }

        await _userManager.UpdateAsync(user);

        ViewBag.Message = "Profile Updated Successfully";

        model.ProfilePicture = user.ProfilePicture;

        return View(model);
    }
}
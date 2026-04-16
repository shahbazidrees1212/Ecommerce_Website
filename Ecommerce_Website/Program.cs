using Ecommerce_Website.Data;
using Ecommerce_Website.Models;
using EmailSenderApp.Helper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using NETCore.MailKit.Core;

var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE =================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= IDENTITY =================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // 🔐 Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // 🔐 Lockout settings (optional)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ================= COOKIE =================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// ================= EMAIL SERVICE =================
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ================= MVC =================
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ================= SEED ADMIN =================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedAdminAsync(services);
}

// ================= MIDDLEWARE =================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // ✅ Added
}

app.UseHttpsRedirection(); // ✅ Important
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ================= ROUTING =================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// ================= ADMIN SEED METHOD =================
async Task SeedAdminAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string adminEmail = "admin@gmail.com";
    string adminPassword = "Admin@123";

    // Create roles
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    // Create admin user
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true // ✅ Important
        };

        var result = await userManager.CreateAsync(user, adminPassword);

        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Admin");
    }
}
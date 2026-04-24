using Microsoft.AspNetCore.Identity;

namespace Ecommerce_Website.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsBlocked { get; set; } = false;
    }
}
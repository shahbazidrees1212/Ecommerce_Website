using Microsoft.AspNetCore.Http;

namespace Ecommerce_Website.ViewModels
{
    public class ProfileViewModel
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string ProfilePicture { get; set; }

        public IFormFile ProfileImage { get; set; }
    }
}
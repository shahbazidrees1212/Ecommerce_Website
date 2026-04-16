using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce_Website.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [DisplayName("Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [DisplayName("New Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [DisplayName("Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
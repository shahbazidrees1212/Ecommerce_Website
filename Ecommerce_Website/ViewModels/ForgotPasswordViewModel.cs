using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce_Website.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [DisplayName("Email Address")]
        public string Email { get; set; }
    }
}
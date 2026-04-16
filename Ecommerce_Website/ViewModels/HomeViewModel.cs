using Ecommerce_Website.Models;

namespace Ecommerce_Website.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<Product> Products { get; set; }
    }
}
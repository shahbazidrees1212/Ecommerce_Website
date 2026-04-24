using System.ComponentModel.DataAnnotations;

namespace Ecommerce_Website.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? ImageUrl { get; set; }

        // Navigation Property
        public ICollection<Product>? Products { get; set; }
    }
}
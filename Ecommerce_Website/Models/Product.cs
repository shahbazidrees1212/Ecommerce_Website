using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_Website.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        [NotMapped]
        public IFormFile ImageFile { get; set; }

        public int CategoryId { get; set; }

        public Category Category { get; set; }

        public int Stock { get; set; } = 0;
    }
}
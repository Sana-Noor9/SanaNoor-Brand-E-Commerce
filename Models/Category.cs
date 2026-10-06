using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Slug { get; set; } = string.Empty;        // ✅ YEH HO

        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public ICollection<Category>? SubCategories { get; set; }

        public bool IsActive { get; set; } = true;              // ✅ YEH HO
        public int DisplayOrder { get; set; } = 0;              // ✅ YEH HO
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now; // ✅ YEH HO

        public List<Product>? Products { get; set; }
    }
}
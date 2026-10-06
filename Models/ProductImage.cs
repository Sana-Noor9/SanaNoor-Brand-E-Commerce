using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public class ProductImage
    {
        [Key]
        public int ProductImageId { get; set; }

        [Required]
        public int ProductColorId { get; set; }

        [ForeignKey("ProductColorId")]
        public ProductColor? ProductColor { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;   // ✅ YEH HO

        public int DisplayOrder { get; set; } = 0;              // ✅ YEH HO
        public bool IsPrimary { get; set; } = false;            // ✅ YEH HO
        public DateTime UploadedAt { get; set; } = DateTime.Now;

    }
}
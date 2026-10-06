using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public class ProductSize
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductColorId { get; set; }

        [ForeignKey("ProductColorId")]
        public ProductColor ProductColor { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        [Display(Name = "Size")]
        public string SizeName { get; set; } = string.Empty;
        // Example: "S/M", "L/XL", "Free Size", "28", "30"

        [Range(0, int.MaxValue)]
        public int Stock { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExtraPrice { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }
}
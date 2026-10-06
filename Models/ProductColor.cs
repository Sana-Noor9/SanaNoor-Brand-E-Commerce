using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public class ProductColor
    {
        [Key]
        public int ProductColorId { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        [StringLength(50)]
        public string ColorName { get; set; } = string.Empty;

        [StringLength(10)]
        public string? ColorCode { get; set; } = "#000000";
        public string ColorHex { get; set; } = string.Empty;
        public int Stock { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExtraPrice { get; set; } = 0;
        public string? Sizes { get; set; } = "S,M,L,XL";
        public bool IsDefault { get; set; } = false;

        // ✅ صحیح Navigation Properties
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        // Ye line aapke ProductColor class ke andar honi chahiye
        public ICollection<ProductVideo> Videos { get; set; } = new List<ProductVideo>();
    }
}
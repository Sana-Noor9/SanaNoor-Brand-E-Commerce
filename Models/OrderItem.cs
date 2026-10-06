using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        public int ProductColorId { get; set; }

        [ForeignKey("ProductColorId")]
        public ProductColor? ProductColor { get; set; }

        [Required]
        [StringLength(20)]
        public string Size { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public int Quantity { get; set; }

        // SNAPSHOT FIELDS
        [StringLength(200)]
        public string? ProductName { get; set; }

        [StringLength(50)]
        public string? ColorName { get; set; }
    }
}
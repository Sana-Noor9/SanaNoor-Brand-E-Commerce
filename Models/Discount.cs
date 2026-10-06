using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    public class Discount
    {
        [Key]
        public int DiscountId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public DiscountType Type { get; set; } = DiscountType.Percentage;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(1);

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // 🔴 FIX: YEH METHOD VIEW MEIN USE HO RAHA HAI
        public string GetDisplayValue()
        {
            return Type == DiscountType.Percentage
                ? $"{Value}%"
                : $"₹{Value:N0}";
        }

        public bool IsValid()
        {
            return IsActive &&
                   Value > 0 &&
                   DateTime.Now >= StartDate &&
                   DateTime.Now <= EndDate;
        }
    }
}
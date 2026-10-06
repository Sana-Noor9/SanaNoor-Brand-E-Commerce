using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        // BASIC INFO
        [Required]
        [StringLength(200)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ComparePrice { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ProductCode { get; set; }

        [StringLength(50)]
        public string? Barcode { get; set; }

        // CATEGORY
        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        // DETAILS
        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Fabric { get; set; }

        [StringLength(100)]
        public string? Occasion { get; set; }

        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(50)]
        public string? Pieces { get; set; }

        [StringLength(50)]
        public string? DesignCode { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        // 3 PIECE DETAILS
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShirtLength { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TrouserLength { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DupattaLength { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Weight { get; set; }

        [StringLength(100)]
        public string? ShirtFabric { get; set; }

        [StringLength(100)]
        public string? TrouserFabric { get; set; }

        [StringLength(100)]
        public string? DupattaFabric { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShirtQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TrouserQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DupattaQuantity { get; set; }

        // STOCK
        [Required]
        public int StockQuantity { get; set; }

        // DESCRIPTIONS
        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        [StringLength(500)]
        public string? CareInstruction { get; set; }

        [StringLength(500)]
        public string? Disclaimer { get; set; }

        // STATUS
        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string Availability { get; set; } = "in-stock";

        // AUDIT
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // NAVIGATION (✅ Simplified initialization)
        public ICollection<ProductImage> Images { get; set; } = [];
        public ICollection<ProductColor> ProductColors { get; set; } = [];
        public Discount? Discount { get; set; }
        public ICollection<ProductVideo> Videos { get; set; } = [];

        [NotMapped]
        public object? ProductImages { get; set; }


        // ============================================
        // 🔥 NOT MAPPED PROPERTIES
        // ============================================

        [NotMapped]
        public string? PrimaryImagePath
        {
            get
            {
                var primary = ProductColors?
                    .SelectMany(pc => pc.Images ?? [])
                    .FirstOrDefault(pi => pi.IsPrimary);

                if (primary != null)
                    return primary.ImagePath;

                var any = ProductColors?
                    .SelectMany(pc => pc.Images ?? [])
                    .FirstOrDefault();

                return any?.ImagePath;
            }
        }

        [NotMapped]
        public bool HasDiscount => Discount != null && Discount.IsActive && Discount.Value > 0;

        [NotMapped]
        public int DiscountPercentage
        {
            get
            {
                if (!HasDiscount || Price <= 0) return 0;

                return Discount!.Type == DiscountType.Percentage
                    ? (int)Math.Floor(Discount.Value)
                    : (int)Math.Floor((Discount.Value / Price) * 100);
            }
        }

        [NotMapped]
        public decimal SavedAmount => Price - GetDiscountedPrice();

        [NotMapped]
        public string FormattedPrice => $"₹{Price:N0}";

        [NotMapped]
        public string FormattedDiscountedPrice => $"₹{GetDiscountedPrice():N0}";

        [NotMapped]
        public string FormattedSavedAmount => $"₹{SavedAmount:N0}";

        [NotMapped]
        public int TotalColorStock
        {
            get
            {
                if (ProductColors is not { Count: > 0 })
                    return StockQuantity;

                return ProductColors.Sum(c => c.Stock);
            }
        }

        [NotMapped]
        public bool IsAnyColorInStock
        {
            get
            {
                if (ProductColors is not { Count: > 0 })
                    return StockQuantity > 0;

                return ProductColors.Count(c => c.Stock > 0) > 0;
            }
        }

        [NotMapped]
        public List<string> AllAvailableSizes
        {
            get
            {
                if (ProductColors is not { Count: > 0 })
                    return [];

                return ProductColors
                    .Where(c => !string.IsNullOrEmpty(c.Sizes))
                    .SelectMany(c => c.Sizes.Split(',').Select(s => s.Trim()))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();
            }
        }

        [NotMapped]
        public decimal MinPrice
        {
            get
            {
                var colorExtra = ProductColors?.Min(c => c.ExtraPrice) ?? 0m; // ✅ Fixed: 0m use kiya
                return GetDiscountedPrice() + colorExtra;
            }
        }

        [NotMapped]
        public decimal MaxPrice
        {
            get
            {
                var colorExtra = ProductColors?.Max(c => c.ExtraPrice) ?? 0m; // ✅ Fixed: 0m use kiya
                return GetDiscountedPrice() + colorExtra;
            }
        }

        [NotMapped]
        public string PriceRange
        {
            get
            {
                if (MinPrice == MaxPrice)
                    return FormattedDiscountedPrice;

                return $"{FormattedDiscountedPrice} - ₹{MaxPrice:N0}"; // ✅ Fixed: Simplified interpolation
            }
        }


        // ============================================
        // 🔥 METHODS
        // ============================================

        public void UpdateAvailability()
        {
            Availability = StockQuantity > 0 ? "in-stock" : "out-of-stock";
        }

        public decimal GetDiscountedPrice()
        {
            if (!HasDiscount) return Price;

            var discounted = Discount!.Type == DiscountType.Percentage
                ? Price - (Price * Discount.Value / 100)
                : Price - Discount.Value;

            return discounted < 0 ? 0 : discounted;
        }

        public decimal GetDiscountAmount()
        {
            if (!HasDiscount) return 0;

            return Discount!.Type == DiscountType.Percentage
                ? Price * Discount.Value / 100
                : Discount.Value;
        }

        public decimal GetPriceForColor(ProductColor? color)
        {
            if (color == null)
                return GetDiscountedPrice();

            return GetDiscountedPrice() + (color.ExtraPrice ?? 0m); // ✅ Fixed: 0m use kiya
        }
    }
}
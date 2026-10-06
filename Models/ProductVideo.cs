using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanaNoor_Brand.Models
{
    public class ProductVideo
    {
        [Key]
        public int ProductVideoId { get; set; }

        [Required]
        public string VideoPath { get; set; } = string.Empty;

        // Humne iska naam 'Title' rakh diya taake controller se match kare
        public string? Title { get; set; }

        // Ye missing tha, is liye error aa raha tha
        public string VideoType { get; set; } = "Upload";

        public bool IsActive { get; set; } = true;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int? ProductColorId { get; set; }

        [ForeignKey("ProductColorId")]
        public ProductColor? ProductColor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
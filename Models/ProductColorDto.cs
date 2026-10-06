using System.Collections.Generic;

namespace SanaNoor_Brand.Models
{
    public class ProductColorDto
    {
        public string ColorName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal? ExtraPrice { get; set; }
        public string ColorCode { get; set; } = "#000000";
        public string? Sizes { get; set; }
        public List<string> Images { get; set; } = new List<string>();
    }
}
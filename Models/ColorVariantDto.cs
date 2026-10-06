using System.Collections.Generic;

namespace SanaNoor_Brand.Models
{
    public class ColorVariantDto
    {
        public string ColorCode { get; set; } = "#000000";
        public int Stock { get; set; }
        public decimal ExtraPrice { get; set; }
        public List<string> Sizes { get; set; } = new List<string>();
        public List<string> Images { get; set; } = new List<string>();
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Fontana.AI.Models
{
    public class DabasProduct
    {
        public string Gtin { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Ingredients { get; set; } = string.Empty;
        public string Allergens { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Nutrition { get; set; } = string.Empty;
    }
}

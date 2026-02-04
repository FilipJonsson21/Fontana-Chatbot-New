using System;
using System.Collections.Generic;
using System.Text;

namespace Fontana.AI.Models
{
    public class FaqItem
    {
        public int Id { get; set; }
        public required string Question { get; set; }

        public required string Answer { get; set; }

        // lägga till en kategor senare för merch eller öppettider.

        public string? Category { get; set; }

    }
}

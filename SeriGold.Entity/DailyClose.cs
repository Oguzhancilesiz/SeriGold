using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Entity
{
    // Entities/DailyClose.cs
    public sealed class DailyClose
    {
        public int Id { get; set; }
        public DateOnly TrDate { get; set; }          // TR günü
        public string Symbol { get; set; } = null!;
        public decimal Mid { get; set; }              // kapanış ref
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

}

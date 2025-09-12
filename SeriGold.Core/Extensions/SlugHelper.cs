using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SeriGold.Core.Extensions
{
    public static class SlugHelper
    {
        public static string Make(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant()
                .Replace("ğ", "g").Replace("ü", "u").Replace("ş", "s")
                .Replace("ı", "i").Replace("ö", "o").Replace("ç", "c");
            s = Regex.Replace(s, @"[^a-z0-9\-]+", "-");
            s = Regex.Replace(s, @"-+", "-").Trim('-');
            return s;
        }
    }
}

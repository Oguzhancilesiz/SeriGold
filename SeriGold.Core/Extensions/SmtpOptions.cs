using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Core.Extensions
{
    public sealed class SmtpOptions
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string UserName { get; set; } = "";   // Gmail için genelde FromEmail ile aynı
        public string Password { get; set; } = "";   // App Password (2FA aktif olmalı)
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "CODE";
        public int TimeoutMs { get; set; } = 100000; // 100 sn
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SeriGold.Core.Extensions
{
    public static class MailHelper
    {
        public static MailMessage MailCreate(
            IEnumerable<string> to,
            string subject,
            string body,
            string? cc = null,
            string? bcc = null,
            string? replyTo = null,
            IEnumerable<Attachment>? attachments = null,
            bool isHtml = true)
        {
            var message = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            foreach (var addr in to.Where(a => !string.IsNullOrWhiteSpace(a)))
                message.To.Add(addr.Trim());

            foreach (var addr in SplitEmails(cc))
                message.CC.Add(addr);

            foreach (var addr in SplitEmails(bcc))
                message.Bcc.Add(addr);

            if (!string.IsNullOrWhiteSpace(replyTo))
                message.ReplyToList.Add(new MailAddress(replyTo!.Trim()));

            if (attachments != null)
            {
                foreach (var a in attachments)
                    message.Attachments.Add(a);
            }

            return message;
        }

        public static async Task SendAsync(MailMessage message, SmtpOptions options, CancellationToken ct = default)
        {
            if (message.From is null || string.IsNullOrWhiteSpace(message.From.Address))
                message.From = new MailAddress(options.FromEmail, options.FromName);

            using (message)
            using (var smtp = new SmtpClient(options.Host, options.Port))
            {
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false; // net ol
                smtp.EnableSsl = options.UseSsl;
                smtp.Credentials = new NetworkCredential(options.UserName, options.Password);
                smtp.Timeout = options.TimeoutMs;

                await smtp.SendMailAsync(message);
            }
        }

        // Eski imzayı korumak istersen (host/port/username/password alan overload)
        public static Task SendAsync(MailMessage message, string host, int portNumber, string username, string password, bool useSsl = true, CancellationToken ct = default)
            => SendAsync(message, new SmtpOptions
            {
                Host = host,
                Port = portNumber,
                UserName = username,
                Password = password,
                FromEmail = username, // Gmail için zorunlu gibi düşünebilirsin
                UseSsl = useSsl
            }, ct);

        private static IEnumerable<string> SplitEmails(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) yield break;
            var parts = s.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var trimmed = p.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    yield return trimmed;
            }
        }
    }
}

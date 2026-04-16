using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EmailSenderApp.Helper
{
    public class EmailSender : IEmailSender
    {
        private readonly string smtpServer = "smtp.gmail.com";
        private readonly int smtpPort = 587;

        private readonly string fromEmail = "shahbazidrees1212@gmail.com";

        // ✅ REMOVE SPACES
        private readonly string fromPassword = "xmhgzivekuqdmepe";

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendAsync(email, subject, htmlMessage);
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(fromEmail, fromPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, "E-Commerce"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(to);

                    Console.WriteLine("🚀 Sending email to: " + to);

                    await client.SendMailAsync(mailMessage);

                    Console.WriteLine("✅ Email Sent Successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ FULL ERROR: " + ex.ToString());
                throw;
            }
        }
    }
}
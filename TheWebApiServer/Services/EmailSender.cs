using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;

namespace TheWebApiServer.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential("juratestmail@gmail.com", "rixr brdn zqiq oaxq")
            };
            MailMessage message = new MailMessage("juratestmail@gmail.com", email, subject, htmlMessage);
            message.IsBodyHtml = true;

            await client.SendMailAsync(message);
        }
    }
}

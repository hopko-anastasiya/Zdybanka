using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Zdybanka.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Dummy implementation using System.Net.Mail
            // Recommend replacing with real credentials or a service like MailKit/SendGrid

            /* 
            var mail = "your-email@gmail.com";
            var pw = "your-app-password";

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(mail, pw)
            };

            return client.SendMailAsync(
                new MailMessage(from: mail, to: email, subject, message)
                {
                    IsBodyHtml = true
                }
            );
            */

             // Returning completed task for now
             return Task.CompletedTask;
        }
    }
}
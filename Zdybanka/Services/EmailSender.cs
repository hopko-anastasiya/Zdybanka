using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Zdybanka.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var mail = "Gopkxf60@gmail.com"; 
            var pw = "nywq tuol khat lokl";

            // 2. Налаштування клієнта
            using (var client = new SmtpClient("smtp.gmail.com", 587))
            {
                client.EnableSsl = true;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(mail, pw);

                // 3. Створення листа
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(mail, "Zdybanka Support"), // Ім'я відправника
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                
                mailMessage.To.Add(email);

                // 4. Відправка
                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
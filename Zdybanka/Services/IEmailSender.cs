using System.Threading.Tasks;

namespace Zdybanka.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
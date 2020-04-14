using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace com.b_velop.Slipways.Web.Infrastructure
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        public AuthMessageSenderOptions Options { get; } //set only via Secret Manager

        public EmailSender(
            AuthMessageSenderOptions optionsAccessor,
            ILogger<EmailSender> logger)
        {
            Options = optionsAccessor;
            _logger = logger;
        }

        public Task SendEmailAsync(
            string email,
            string subject,
            string message)
            => Execute(Options.SendGridKey, subject, message, email);

        public Task Execute(
            string apiKey,
            string subject,
            string message,
            string email)
        {
            message = message.Replace("http://", "https://");
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("marcel.benders@outlook.de", Options.SendGridUser),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            return client.SendEmailAsync(msg);
        }
    }
}
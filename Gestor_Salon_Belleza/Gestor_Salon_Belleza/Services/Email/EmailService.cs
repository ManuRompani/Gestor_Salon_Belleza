using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Gestor_Salon_Belleza.Services.Email
{
    public class EmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
                throw new ArgumentException("El destinatario no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(asunto))
                throw new ArgumentException("El asunto no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(cuerpoHtml))
                throw new ArgumentException("El cuerpo del correo no puede estar vacío.");

            var mensaje = new MimeMessage();

            mensaje.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;

            mensaje.Body = new BodyBuilder
            {
                HtmlBody = cuerpoHtml
            }.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _settings.UserName,
                _settings.Password
            );

            await smtp.SendAsync(mensaje);

            await smtp.DisconnectAsync(true);
        }
    }
}

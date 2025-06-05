using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MiPokemonApp.Models.Email;
using MiPokemonApp.Services.Interfaces;

namespace MiPokemonApp.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly bool _isConfigured;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;

            // Validamos si hay configuración para SMTP
            _isConfigured = !string.IsNullOrEmpty(_settings.SmtpHost)
                            && !string.IsNullOrEmpty(_settings.SmtpUser)
                            && !string.IsNullOrEmpty(_settings.SmtpPass)
                            && _settings.SmtpPort > 0;
        }

        public async Task SendEmailAsync(string toAddress, string subject, string body)
        {
            if (_isConfigured)
            {
                var smtp = new SmtpClient
                {
                    Host = _settings.SmtpHost,
                    Port = _settings.SmtpPort,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_settings.SmtpUser, "Mi App Pokémon"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                message.To.Add(toAddress);

                await smtp.SendMailAsync(message);
            }
            else
            {
                // Modo simulación
                Console.WriteLine($"[Simulación] Enviando correo a {toAddress}");
                Console.WriteLine($"Asunto: {subject}");
                Console.WriteLine($"Cuerpo: {body}");
                Console.WriteLine("---");

                await Task.CompletedTask;
            }
        }

        public async Task SendBulkEmailAsync(List<string> toAddresses, string subject, string body)
        {
            foreach (var addr in toAddresses)
            {
                await SendEmailAsync(addr, subject, body);
            }
        }
    }
}

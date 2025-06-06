using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiPokemonApp.Models.Email;
using MiPokemonApp.Services.Interfaces;

namespace MiPokemonApp.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly bool _isConfigured;
        private readonly ILogger<EmailService> _logger;

        private const string DEFAULT_FROM_DISPLAY_NAME = "Mi App Pokémon";
        private const string ERROR_INVALID_EMAIL = "El correo electrónico de destino no es válido.";
        private const string ERROR_INVALID_SUBJECT = "El asunto no puede estar vacío.";
        private const string ERROR_INVALID_BODY = "El cuerpo del correo no puede estar vacío.";
        private const string ERROR_INVALID_BULK_LIST = "La lista de destinatarios está vacía.";

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            _isConfigured =
                !string.IsNullOrWhiteSpace(_settings.SmtpHost) &&
                _settings.SmtpPort > 0 &&
                !string.IsNullOrWhiteSpace(_settings.SmtpUser) &&
                !string.IsNullOrWhiteSpace(_settings.SmtpPass);

            if (!_isConfigured)
            {
                _logger.LogWarning("La configuración SMTP no está completa. Modo simulación habilitado.");
            }
        }

        public bool ValidateEmailFields(string? emailList, string? subject, string? body)
        {
            return
                !string.IsNullOrWhiteSpace(emailList) &&
                !string.IsNullOrWhiteSpace(subject) &&
                !string.IsNullOrWhiteSpace(body);
        }

        public List<string> ParseAndValidateEmailList(string emailList)
        {
            if (string.IsNullOrWhiteSpace(emailList))
                return new List<string>();

            var candidatos = emailList
                .Replace("\r", "")
                .Replace("\n", "")
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct();

            var válidos = new List<string>();

            foreach (var e in candidatos)
            {
                if (IsValidEmail(e))
                    válidos.Add(e);
                else
                    _logger.LogWarning("Dirección inválida ignorada: {Email}", e);
            }

            return válidos;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendEmailAsync(string toAddress, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                _logger.LogError("Intento de enviar correo con dirección vacía.");
                throw new ArgumentException(ERROR_INVALID_EMAIL, nameof(toAddress));
            }
            if (string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogError("Intento de enviar correo con asunto vacío.");
                throw new ArgumentException(ERROR_INVALID_SUBJECT, nameof(subject));
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("Intento de enviar correo con cuerpo vacío.");
                throw new ArgumentException(ERROR_INVALID_BODY, nameof(body));
            }

            // 1) Validar formato del destinatario
            var destinatarioLimpio = toAddress.Trim();
            if (!IsValidEmail(destinatarioLimpio))
            {
                _logger.LogWarning("Dirección inválida detectada en SendEmailAsync; se omite: {Email}", destinatarioLimpio);
                return;
            }

            // 2) Validar que la configuración SMTP tenga al menos:
            //    - Host no vacío
            //    - Puerto > 0
            //    - Usuario válido en formato email
            //    - Contraseña no vacía
            var smtpUserValido = !string.IsNullOrWhiteSpace(_settings.SmtpUser) && IsValidEmail(_settings.SmtpUser);
            if (!_isConfigured || !smtpUserValido)
            {
                // Si falta cualquiera, entramos en modo simulación
                _logger.LogWarning("Configuración SMTP incompleta o SmtpUser inválido. Entrando en modo simulación.");
                _logger.LogInformation("[Simulación] Enviando correo a {ToAddress}", destinatarioLimpio);
                Console.WriteLine($"[Simulación] Enviando correo a: {destinatarioLimpio}");
                Console.WriteLine($"Asunto: {subject}");
                Console.WriteLine($"Cuerpo: {body}");
                Console.WriteLine("--------------------------------------------------");
                await Task.CompletedTask;
                return;
            }

            // 3) Si llegamos aquí, sí hay configuración SMTP completa y _settings.SmtpUser es un email válido.
            try
            {
                using var smtp = new SmtpClient
                {
                    Host = _settings.SmtpHost!,
                    Port = _settings.SmtpPort,
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.SmtpUser!, _settings.SmtpPass!)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.SmtpUser!, DEFAULT_FROM_DISPLAY_NAME),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = _settings.IsBodyHtml
                };

                message.To.Add(destinatarioLimpio);
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Correo enviado correctamente a {ToAddress}", destinatarioLimpio);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Error SMTP al enviar correo a {ToAddress}", destinatarioLimpio);
                throw new EmailException($"Error al enviar correo SMTP al destinatario {destinatarioLimpio}.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar correo a {ToAddress}", destinatarioLimpio);
                throw new EmailException($"Error inesperado al enviar correo al destinatario {destinatarioLimpio}.", ex);
            }
        }

        public async Task SendBulkEmailAsync(List<string> toAddresses, string subject, string body)
        {
            if (toAddresses == null || toAddresses.Count == 0)
            {
                _logger.LogError("La lista de destinatarios para envío masivo está vacía.");
                throw new ArgumentException(ERROR_INVALID_BULK_LIST, nameof(toAddresses));
            }

            foreach (var rawAddr in toAddresses)
            {
                if (string.IsNullOrWhiteSpace(rawAddr))
                {
                    _logger.LogWarning("Encontrada dirección vacía en la lista de envío masivo. Se omite.");
                    continue;
                }

                var cleanedAddr = rawAddr.Trim();
                try
                {
                    await SendEmailAsync(cleanedAddr, subject, body);
                }
                catch (EmailException e)
                {
                    _logger.LogError(e, "Falló el envío a {Email}. Continuando con el siguiente.", cleanedAddr);
                }
                catch (ArgumentException argEx)
                {
                    _logger.LogWarning(argEx, "Dirección inválida o datos inválidos para {Email}. Se omite.", cleanedAddr);
                }
            }
        }

        public class EmailException : Exception
        {
            public EmailException() { }
            public EmailException(string message) : base(message) { }
            public EmailException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}

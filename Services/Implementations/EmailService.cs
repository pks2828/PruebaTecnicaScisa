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
    /// <summary>
    /// Servicio para el envío de correos electrónicos, con manejo de errores y validación de parámetros.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly bool _isConfigured;
        // private readonly ILogger<EmailService> _logger; // Descomentar si se inyecta ILogger

        private const string DEFAULT_FROM_DISPLAY_NAME = "Mi App Pokémon";
        private const string ERROR_INVALID_EMAIL = "El correo electrónico de destino no es válido.";
        private const string ERROR_INVALID_SUBJECT = "El asunto no puede estar vacío.";
        private const string ERROR_INVALID_BODY = "El cuerpo del correo no puede estar vacío.";
        private const string ERROR_INVALID_BULK_LIST = "La lista de destinatarios está vacía.";

        public EmailService(IOptions<EmailSettings> settings /*, ILogger<EmailService> logger */)
        {
            _settings = settings.Value;
            // _logger = logger;

            // Validamos la configuración SMTP
            _isConfigured =
                !string.IsNullOrEmpty(_settings.SmtpHost) &&
                !string.IsNullOrEmpty(_settings.SmtpUser) &&
                !string.IsNullOrEmpty(_settings.SmtpPass) &&
                _settings.SmtpPort > 0;
        }

        /// <summary>
        /// Envía un correo electrónico de manera asincrónica a una dirección específica.
        /// </summary>
        /// <param name="toAddress">Dirección de correo electrónico del destinatario.</param>
        /// <param name="subject">Asunto del correo electrónico.</param>
        /// <param name="body">Cuerpo del correo electrónico.</param>
        /// <returns>Tarea que representa la operación asincrónica.</returns>
        /// <exception cref="ArgumentException">Si <paramref name="toAddress"/>, <paramref name="subject"/> o <paramref name="body"/> son nulos o vacíos.</exception>
        /// <exception cref="EmailException">Si ocurre un error al enviar el correo.</exception>
        public async Task SendEmailAsync(string toAddress, string subject, string body)
        {
            // Validación de parámetros de entrada
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new ArgumentException(ERROR_INVALID_EMAIL, nameof(toAddress));
            }
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException(ERROR_INVALID_SUBJECT, nameof(subject));
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentException(ERROR_INVALID_BODY, nameof(body));
            }

            if (_isConfigured)
            {
                try
                {
                    // Creamos y configuramos el cliente SMTP
                    using (var smtp = new SmtpClient
                    {
                        Host = _settings.SmtpHost,
                        Port = _settings.SmtpPort,
                        EnableSsl = true,
                        Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass)
                    })
                    using (var message = new MailMessage
                    {
                        From = new MailAddress(_settings.SmtpUser, DEFAULT_FROM_DISPLAY_NAME),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false
                    })
                    {
                        message.To.Add(toAddress);

                        await smtp.SendMailAsync(message);
                    }
                }
                catch (SmtpException ex)
                {
                    // _logger.LogError(ex, "Error SMTP al enviar correo a {ToAddress}", toAddress);
                    throw new EmailException($"Error al enviar correo SMTP al destinatario {toAddress}.", ex);
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "Error inesperado al enviar correo a {ToAddress}", toAddress);
                    throw new EmailException($"Error inesperado al enviar correo al destinatario {toAddress}.", ex);
                }
            }
            else
            {
                // Modo simulación: se muestra por consola en lugar de enviar realmente el correo
                Console.WriteLine($"[Simulación] Enviando correo a {toAddress}");
                Console.WriteLine($"Asunto: {subject}");
                Console.WriteLine($"Cuerpo: {body}");
                Console.WriteLine("---");

                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Envía correos electrónicos de manera asincrónica a múltiples direcciones.
        /// </summary>
        /// <param name="toAddresses">Lista de direcciones de correo electrónico de los destinatarios.</param>
        /// <param name="subject">Asunto de los correos electrónicos.</param>
        /// <param name="body">Cuerpo de los correos electrónicos.</param>
        /// <returns>Tarea que representa la operación asincrónica.</returns>
        /// <exception cref="ArgumentException">Si <paramref name="toAddresses"/> es nulo o está vacío.</exception>
        public async Task SendBulkEmailAsync(List<string> toAddresses, string subject, string body)
        {
            // Validación de lista de destinatarios
            if (toAddresses == null || toAddresses.Count == 0)
            {
                throw new ArgumentException(ERROR_INVALID_BULK_LIST, nameof(toAddresses));
            }

            foreach (var addr in toAddresses)
            {
                // Validación de cada dirección antes de enviar
                if (string.IsNullOrWhiteSpace(addr))
                {
                    // _logger.LogWarning("Dirección de correo vacía en la lista de envío masivo.");
                    continue;
                }

                await SendEmailAsync(addr, subject, body);
            }
        }

        /// <summary>
        /// Excepción personalizada para errores en el envío de correos electrónicos.
        /// </summary>
        public class EmailException : Exception
        {
            public EmailException() { }

            public EmailException(string message) : base(message) { }

            public EmailException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}

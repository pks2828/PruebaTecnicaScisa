namespace MiPokemonApp.Services.Interfaces
{
    /// <summary>
    /// Interfaz que define las operaciones de envío de correos electrónicos,
    /// tanto individual como masivo, para la aplicación.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envía un correo electrónico a una única dirección.
        /// </summary>
        /// <param name="toAddress">Dirección de correo del destinatario.</param>
        /// <param name="subject">Asunto del correo.</param>
        /// <param name="body">Cuerpo del mensaje.</param>
        Task SendEmailAsync(string toAddress, string subject, string body);

        /// <summary>
        /// Envía correos electrónicos a múltiples direcciones de manera secuencial.
        /// </summary>
        /// <param name="toAddresses">Lista de correos electrónicos de los destinatarios.</param>
        /// <param name="subject">Asunto del correo.</param>
        /// <param name="body">Cuerpo del mensaje.</param>
        Task SendBulkEmailAsync(List<string> toAddresses, string subject, string body);

        /// <summary>
        /// Valida que los campos requeridos (lista de correos, asunto y cuerpo) estén presentes.
        /// </summary>
        bool ValidateEmailFields(string? emailList, string? subject, string? body);

        /// <summary>
        /// Recibe una cadena con correos separados por comas, puntos y comas o espacios.
        /// Devuelve solo las direcciones que tienen un formato válido según MailAddress.
        /// </summary>
        List<string> ParseAndValidateEmailList(string emailList);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiPokemonApp.Services.Interfaces;

namespace MiPokemonApp.Services.Implementations
{
    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(string toAddress, string subject, string body)
        {
            // Simulación de envío: imprimimos en consola
            Console.WriteLine($"[Simulación] Enviando correo a {toAddress}");
            Console.WriteLine($"Asunto: {subject}");
            Console.WriteLine($"Cuerpo: {body}");
            Console.WriteLine("---");

            await Task.CompletedTask;
        }

        public async Task SendBulkEmailAsync(List<string> toAddresses, string subject, string body)
        {
            foreach (var addr in toAddresses)
            {
                Console.WriteLine($"[Simulación] Enviando correo a {addr}");
                Console.WriteLine($"Asunto: {subject}");
                Console.WriteLine($"Cuerpo: {body}");
                Console.WriteLine("---");
            }

            await Task.CompletedTask;
        }
    }
}

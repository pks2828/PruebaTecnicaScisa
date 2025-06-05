namespace MiPokemonApp.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toAddress, string subject, string body);
        Task SendBulkEmailAsync(List<string> toAddresses, string subject, string body);
    }
}

namespace MiPokemonApp.Models.Email
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPass { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public bool IsBodyHtml { get; set; } = false;



    }
}

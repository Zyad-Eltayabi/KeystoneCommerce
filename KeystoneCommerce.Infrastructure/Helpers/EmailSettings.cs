namespace KeystoneCommerce.Infrastructure.Helpers
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string From { get; set; } = string.Empty;
    }
}
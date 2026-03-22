namespace EMenu.Infrastructure.Configurations
{
    public class VNPayConfig
    {
        public string TmnCode { get; set; } = string.Empty;

        public string HashSecret { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = string.Empty;
    }
}

namespace EMenu.Web.ViewModels
{
    public class VNPayReturnData
    {
        public string TxnRef { get; set; } = string.Empty;

        public string ResponseCode { get; set; } = string.Empty;

        public string TransactionStatus { get; set; } = string.Empty;

        public string SecureHash { get; set; } = string.Empty;

        public string? TransactionNo { get; set; }

        public string? PayDate { get; set; }
    }
}

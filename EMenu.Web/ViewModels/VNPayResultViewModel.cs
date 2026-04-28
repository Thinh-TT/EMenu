namespace EMenu.Web.ViewModels
{
    public class VNPayResultViewModel
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;

        public string TxnRef { get; set; } = string.Empty;

        public string ResponseCode { get; set; } = string.Empty;

        public string TransactionStatus { get; set; } = string.Empty;

        public int? SessionId { get; set; }

        public int? OrderId { get; set; }

        public string? TransactionNo { get; set; }

        public string? PayDate { get; set; }
    }
}

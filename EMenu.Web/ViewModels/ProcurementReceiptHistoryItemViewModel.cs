namespace EMenu.Web.ViewModels
{
    public class ProcurementReceiptHistoryItemViewModel
    {
        public int ReceiptId { get; set; }

        public DateTime CreatedDate { get; set; }

        public int SupplierId { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public int StaffId { get; set; }

        public string StaffName { get; set; } = string.Empty;

        public int ItemCount { get; set; }

        public decimal TotalAmount { get; set; }

        public IReadOnlyList<ProcurementReceiptLineInputViewModel> Items { get; set; } = [];
    }
}

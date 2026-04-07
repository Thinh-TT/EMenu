using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class ProcurementReceiptHistoryViewModel
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int? SupplierId { get; set; }

        public IReadOnlyList<Supplier> Suppliers { get; set; } = [];

        public IReadOnlyList<ProcurementReceiptHistoryItemViewModel> Receipts { get; set; } = [];

        public decimal TotalAmount { get; set; }
    }
}

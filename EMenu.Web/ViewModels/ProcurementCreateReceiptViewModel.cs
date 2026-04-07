using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class ProcurementCreateReceiptViewModel
    {
        public int? SupplierId { get; set; }

        public int? StaffId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public IReadOnlyList<Supplier> Suppliers { get; set; } = [];

        public IReadOnlyList<Staff> Staffs { get; set; } = [];

        public IReadOnlyList<Ingredient> Ingredients { get; set; } = [];

        public IReadOnlyList<ProcurementReceiptLineInputViewModel> Items { get; set; }
            = [new ProcurementReceiptLineInputViewModel()];
    }
}

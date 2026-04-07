using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class SupplierIndexViewModel
    {
        public string? Keyword { get; set; }

        public IReadOnlyList<Supplier> Suppliers { get; set; } = [];
    }
}

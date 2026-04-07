namespace EMenu.Domain.Entities
{
    public class Supplier
    {
        public int SupplierID { get; set; }

        public string Name { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public ICollection<Receipt> Receipts { get; set; }
    }
}

namespace EMenu.Domain.Entities
{
    public class Receipt
    {
        public int ReceiptID { get; set; }

        public int SupplierID { get; set; }

        public int StaffID { get; set; }

        public DateTime CreatedDate { get; set; }

        public Supplier Supplier { get; set; }

        public Staff Staff { get; set; }

        public ICollection<ReceiptIngredient> ReceiptIngredients { get; set; }
    }
}

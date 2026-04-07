namespace EMenu.Application.Abstractions.DTOs
{
    public class ProcurementReceiptResultDto
    {
        public int ReceiptId { get; set; }

        public int SupplierId { get; set; }

        public int StaffId { get; set; }

        public DateTime CreatedDate { get; set; }

        public int ItemCount { get; set; }

        public decimal TotalAmount { get; set; }
    }
}

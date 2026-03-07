using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class Invoice
    {
        public int InvoiceID { get; set; }

        public DateTime CreatedDate { get; set; }

        public decimal TotalAmount { get; set; }

        public int OrderID { get; set; }

        public Order Order { get; set; }

        public ICollection<Payment> Payments { get; set; }
    }
}

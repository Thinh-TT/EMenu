using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class Payment
    {
        public int PaymentID { get; set; }

        public string Method { get; set; }

        public decimal Amount { get; set; }

        public int Status { get; set; }

        public DateTime PaymentTime { get; set; }

        public int InvoiceID { get; set; }

        public Invoice Invoice { get; set; }
    }
}

using EMenu.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class Order
    {
        public int OrderID { get; set; }

        public OrderStatus Status { get; set; }

        public DateTime CreatedTime { get; set; }

        public decimal TotalAmount { get; set; }

        public int OrderSessionID { get; set; }

        public int StaffID { get; set; }

        public OrderSession OrderSession { get; set; }

        public Staff Staff { get; set; }

        public ICollection<OrderProduct> OrderProducts { get; set; }

        public Invoice Invoice { get; set; }
    }
}

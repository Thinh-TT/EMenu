using EMenu.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class OrderProduct
    {
        public int OrderProductID { get; set; }

        public int OrderID { get; set; }

        public int ProductID { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public OrderItemStatus Status { get; set; }

        public Order Order { get; set; }

        public Product Product { get; set; }
    }
}

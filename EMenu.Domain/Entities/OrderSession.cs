using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class OrderSession
    {
        public int OrderSessionID { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int Status { get; set; }

        public int TableID { get; set; }

        public int CustomerID { get; set; }

        public RestaurantTable RestaurantTable { get; set; }

        public Customer Customer { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}

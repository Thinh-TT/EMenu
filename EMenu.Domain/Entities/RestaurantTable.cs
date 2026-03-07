using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class RestaurantTable
    {
        public int TableID { get; set; }

        public string TableName { get; set; }

        public int Capacity { get; set; }

        public int Status { get; set; }

        public ICollection<OrderSession> OrderSessions { get; set; }
    }
}

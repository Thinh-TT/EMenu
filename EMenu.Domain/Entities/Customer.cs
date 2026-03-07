using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class Customer
    {
        public int CustomerID { get; set; }

        public string Name { get; set; }

        public string Sex { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int? BirthYear { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<OrderSession> OrderSessions { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class Staff
    {
        public int StaffID { get; set; }

        public string StaffName { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public int UserID { get; set; }

        public User User { get; set; }

        public ICollection<ShiftLog> ShiftLogs { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}

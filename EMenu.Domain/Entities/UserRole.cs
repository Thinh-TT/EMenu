using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class UserRole
    {
        public int UserRoleID { get; set; }

        public int UserID { get; set; }

        public int RoleID { get; set; }

        public User User { get; set; }

        public Role Role { get; set; }
    }
}

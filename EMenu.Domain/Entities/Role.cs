using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class Role
    {
        public int RoleID { get; set; }

        public string RoleName { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}

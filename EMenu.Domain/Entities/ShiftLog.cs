using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class ShiftLog
    {
        public int ShiftLogID { get; set; }

        public int StaffID { get; set; }

        public int ShiftID { get; set; }

        public Staff Staff { get; set; }

        public Shift Shift { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Enums
{
    public enum OrderItemStatus
    {
        Pending = 0,
        Preparing = 1,
        Ready = 2,
        Served = 3,
        Cancelled = 4
    }
}

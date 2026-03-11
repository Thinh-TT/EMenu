using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Application.DTOs
{
    public class BillDto
    {
        public int OrderId { get; set; }

        public string TableName { get; set; }

        public List<BillItemDto> Items { get; set; }

        public decimal TotalAmount { get; set; }
    }
}

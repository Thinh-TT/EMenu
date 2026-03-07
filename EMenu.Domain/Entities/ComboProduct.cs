using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class ComboProduct
    {
        public int ComboProductID { get; set; }

        public int ComboID { get; set; }

        public int ProductID { get; set; }

        public int Quantity { get; set; }

        public Product Combo { get; set; }

        public Product Product { get; set; }
    }
}

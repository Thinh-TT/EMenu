using EMenu.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Domain.Entities
{
    public class Product
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; }

        public string Image { get; set; }

        public decimal Price { get; set; }

        public string Description { get; set; }

        public bool IsAvailable { get; set; }

        public ProductType ProductType { get; set; }

        public int CategoryID { get; set; }

        public Category Category { get; set; }

        public ICollection<ComboProduct> ComboProducts { get; set; }

        public ICollection<ComboProduct> ComboItems { get; set; }
    }
}

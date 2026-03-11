using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Infrastructure.Configurations
{
    public class VNPayConfig
    {
        public string TmnCode { get; set; }

        public string HashSecret { get; set; }

        public string Url { get; set; }

        public string ReturnUrl { get; set; }
    }
}

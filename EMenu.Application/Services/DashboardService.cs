using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Application.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public decimal GetTodayRevenue()
        {
            var today = DateTime.Today;

            return _context.Invoices
                .Where(x => x.CreatedDate.Date == today)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0;
        }

        public List<object> GetTopProducts()
        {
            return _context.OrderProducts
                .Include(x => x.Product)
                .GroupBy(x => x.Product.ProductName)
                .Select(g => new
                {
                    Product = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList<object>();
        }

        public object GetTableStatus()
        {
            var total = _context.RestaurantTables.Count();
            var occupied = _context.RestaurantTables.Count(x => x.Status == 1);

            return new
            {
                totalTables = total,
                occupiedTables = occupied
            };
        }
    }
}

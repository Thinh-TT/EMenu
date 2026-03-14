using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Application.Services
{
    public class TableService
    {
        private readonly AppDbContext _context;

        public TableService(AppDbContext context)
        {
            _context = context;
        }

        public List<RestaurantTable> GetAll()
        {
            return _context.RestaurantTables.ToList();
        }

        public RestaurantTable GetById(int id)
        {
            return _context.RestaurantTables.Find(id);
        }

        public void UpdateStatus(int tableId, int status)
        {
            var table = _context.RestaurantTables.Find(tableId);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            table.Status = status;

            _context.SaveChanges();
        }
    }
}

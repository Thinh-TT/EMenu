using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly AppDbContext _context;

        public TableRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<RestaurantTable> GetAll()
        {
            return _context.RestaurantTables.ToList();
        }

        public RestaurantTable? GetById(int tableId)
        {
            return _context.RestaurantTables.FirstOrDefault(x => x.TableID == tableId);
        }

        public int Count()
        {
            return _context.RestaurantTables.Count();
        }

        public int CountInUse()
        {
            return _context.RestaurantTables.Count(x => x.Status == 1);
        }

        public void Update(RestaurantTable table)
        {
            _context.RestaurantTables.Update(table);
        }
    }
}

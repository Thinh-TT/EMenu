using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class WageRepository : IWageRepository
    {
        private readonly AppDbContext _context;

        public WageRepository(AppDbContext context)
        {
            _context = context;
        }

        public Wage? GetById(int id)
        {
            return _context.Wages
                .Include(x => x.Staff)
                .FirstOrDefault(x => x.Id == id);
        }

        public Wage? GetByStaffId(int staffId)
        {
            return _context.Wages
                .Include(x => x.Staff)
                .FirstOrDefault(x => x.StaffID == staffId);
        }

        public IReadOnlyList<Wage> GetAllWithStaff()
        {
            return _context.Wages
                .Include(x => x.Staff)
                .OrderBy(x => x.StaffID)
                .ToList();
        }

        public void Add(Wage wage)
        {
            _context.Wages.Add(wage);
        }

        public void Update(Wage wage)
        {
            _context.Wages.Update(wage);
        }
    }
}

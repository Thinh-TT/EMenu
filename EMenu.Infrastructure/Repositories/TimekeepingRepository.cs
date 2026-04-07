using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class TimekeepingRepository : ITimekeepingRepository
    {
        private readonly AppDbContext _context;

        public TimekeepingRepository(AppDbContext context)
        {
            _context = context;
        }

        public Timekeeping? GetById(int id)
        {
            return _context.Timekeepings
                .Include(x => x.Staff)
                .FirstOrDefault(x => x.Id == id);
        }

        public Timekeeping? GetByStaffAndDate(int staffId, DateOnly date)
        {
            return _context.Timekeepings
                .Include(x => x.Staff)
                .FirstOrDefault(x => x.StaffID == staffId && x.Date == date);
        }

        public IReadOnlyList<Timekeeping> GetByStaffAndMonth(int staffId, int year, int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return _context.Timekeepings
                .Include(x => x.Staff)
                .Where(x =>
                    x.StaffID == staffId &&
                    x.Date >= startDate &&
                    x.Date < endDate)
                .OrderBy(x => x.Date)
                .ToList();
        }

        public IReadOnlyList<Timekeeping> GetByMonth(int year, int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return _context.Timekeepings
                .Include(x => x.Staff)
                .Where(x =>
                    x.Date >= startDate &&
                    x.Date < endDate)
                .OrderBy(x => x.StaffID)
                .ThenBy(x => x.Date)
                .ToList();
        }

        public void Add(Timekeeping timekeeping)
        {
            _context.Timekeepings.Add(timekeeping);
        }

        public void Update(Timekeeping timekeeping)
        {
            _context.Timekeepings.Update(timekeeping);
        }
    }
}

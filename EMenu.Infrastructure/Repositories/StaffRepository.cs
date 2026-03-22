using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly AppDbContext _context;

        public StaffRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Staff> GetAllWithUser()
        {
            return _context.Staffs
                .Include(x => x.User)
                .ToList();
        }

        public Staff? GetById(int staffId)
        {
            return _context.Staffs.FirstOrDefault(x => x.StaffID == staffId);
        }

        public Staff? GetByIdWithUser(int staffId)
        {
            return _context.Staffs
                .Include(x => x.User)
                .FirstOrDefault(x => x.StaffID == staffId);
        }

        public Staff? GetSystemStaff()
        {
            return _context.Staffs.FirstOrDefault(x => x.StaffName == "System");
        }

        public Staff? GetFirstStaff()
        {
            return _context.Staffs
                .OrderBy(x => x.StaffID)
                .FirstOrDefault();
        }

        public void Add(Staff staff)
        {
            _context.Staffs.Add(staff);
        }

        public void Update(Staff staff)
        {
            _context.Staffs.Update(staff);
        }

        public void Remove(Staff staff)
        {
            _context.Staffs.Remove(staff);
        }
    }
}

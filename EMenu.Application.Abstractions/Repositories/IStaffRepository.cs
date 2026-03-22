using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IStaffRepository
    {
        IReadOnlyList<Staff> GetAllWithUser();
        Staff? GetById(int staffId);
        Staff? GetByIdWithUser(int staffId);
        Staff? GetSystemStaff();
        Staff? GetFirstStaff();
        void Add(Staff staff);
        void Update(Staff staff);
        void Remove(Staff staff);
    }
}

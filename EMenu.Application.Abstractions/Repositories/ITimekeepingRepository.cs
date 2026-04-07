using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface ITimekeepingRepository
    {
        Timekeeping? GetById(int id);
        Timekeeping? GetByStaffAndDate(int staffId, DateOnly date);
        IReadOnlyList<Timekeeping> GetByStaffAndMonth(int staffId, int year, int month);
        IReadOnlyList<Timekeeping> GetByMonth(int year, int month);
        void Add(Timekeeping timekeeping);
        void Update(Timekeeping timekeeping);
    }
}

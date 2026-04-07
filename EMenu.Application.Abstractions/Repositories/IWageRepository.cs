using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IWageRepository
    {
        Wage? GetById(int id);
        Wage? GetByStaffId(int staffId);
        IReadOnlyList<Wage> GetAllWithStaff();
        void Add(Wage wage);
        void Update(Wage wage);
    }
}

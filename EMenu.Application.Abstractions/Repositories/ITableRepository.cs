using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface ITableRepository
    {
        IReadOnlyList<RestaurantTable> GetAll();
        RestaurantTable? GetById(int tableId);
        int Count();
        int CountInUse();
        void Update(RestaurantTable table);
    }
}

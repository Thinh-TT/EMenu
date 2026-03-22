using EMenu.Application.Abstractions.DTOs;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IComboRepository
    {
        IReadOnlyList<int> GetComboItemIds(int comboId);
        IReadOnlyList<ComboItemDto> GetComboItems(int comboId);
        void RemoveItemsByComboId(int comboId);
        void AddItems(int comboId, IReadOnlyList<int> productIds);
    }
}

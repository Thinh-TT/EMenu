using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class TableService
    {
        private readonly ITableRepository _tableRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TableService(
            ITableRepository tableRepository,
            IUnitOfWork unitOfWork)
        {
            _tableRepository = tableRepository;
            _unitOfWork = unitOfWork;
        }

        public List<RestaurantTable> GetAll()
        {
            return _tableRepository.GetAll().ToList();
        }

        public RestaurantTable GetById(int id)
        {
            return _tableRepository.GetById(id);
        }

        public void UpdateStatus(int tableId, int status)
        {
            var table = _tableRepository.GetById(tableId);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            table.Status = status;
            _tableRepository.Update(table);
            _unitOfWork.SaveChanges();
        }
    }
}

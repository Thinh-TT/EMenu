using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Repositories;

namespace EMenu.Application.Services
{
    public class DashboardService
    {
        private const int DefaultTopProductsCount = 5;

        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ITableRepository _tableRepository;

        public DashboardService(
            IPaymentRepository paymentRepository,
            IOrderItemRepository orderItemRepository,
            IOrderRepository orderRepository,
            ITableRepository tableRepository)
        {
            _paymentRepository = paymentRepository;
            _orderItemRepository = orderItemRepository;
            _orderRepository = orderRepository;
            _tableRepository = tableRepository;
        }

        public decimal GetTodayRevenue()
        {
            return _paymentRepository.GetRevenueByDate(DateTime.Today);
        }

        public IReadOnlyList<DashboardTopProductDto> GetTopProducts()
        {
            return _orderItemRepository.GetTopProducts(DefaultTopProductsCount);
        }

        public TableStatusSummaryDto GetTableStatus()
        {
            return new TableStatusSummaryDto
            {
                TotalTables = _tableRepository.Count(),
                OccupiedTables = _tableRepository.CountInUse()
            };
        }

        public int GetTodayOrderCount()
        {
            return _orderRepository.CountByCreatedDate(DateTime.Today);
        }

        public int GetTablesInUseCount()
        {
            return _tableRepository.CountInUse();
        }
    }
}

using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Infrastructure.Persistence;
using EMenu.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EMenu.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IComboRepository, ComboRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ISessionRepository, SessionRepository>();
            services.AddScoped<IStaffRepository, StaffRepository>();
            services.AddScoped<ITableRepository, TableRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}

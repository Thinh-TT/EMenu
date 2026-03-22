using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface ICustomerRepository
    {
        bool Exists(int customerId);
        Customer? GetById(int customerId);
        void Add(Customer customer);
    }
}

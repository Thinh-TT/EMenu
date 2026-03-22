using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public bool Exists(int customerId)
        {
            return _context.Customers.Any(x => x.CustomerID == customerId);
        }

        public Customer? GetById(int customerId)
        {
            return _context.Customers.FirstOrDefault(x => x.CustomerID == customerId);
        }

        public void Add(Customer customer)
        {
            _context.Customers.Add(customer);
        }
    }
}

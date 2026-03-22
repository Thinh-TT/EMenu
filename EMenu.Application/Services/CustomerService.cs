using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class CustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CustomerService(
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
        }

        public Customer Create(string name, string? phone, string? email)
        {
            var customer = new Customer
            {
                Name = name,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                CreatedAt = DateTime.Now
            };

            _customerRepository.Add(customer);
            _unitOfWork.SaveChanges();

            return customer;
        }

        public Customer GetById(int id)
        {
            return _customerRepository.GetById(id);
        }
    }
}

using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Application.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context;
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

            _context.Customers.Add(customer);

            _context.SaveChanges();

            return customer;
        }

        public Customer GetById(int id)
        {
            return _context.Customers.Find(id);
        }
    }
}

using EMenu.Application.Abstractions.Persistence;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Persistence
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public EfUnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public ITransaction BeginTransaction()
        {
            return new EfTransaction(_context.Database.BeginTransaction());
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }
    }
}

using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Category> GetAll()
        {
            return _context.Categories.ToList();
        }

        public IReadOnlyList<Category> GetAllWithProducts()
        {
            return _context.Categories
                .Include(x => x.Products)
                .ToList();
        }

        public Category? GetById(int categoryId)
        {
            return _context.Categories.FirstOrDefault(x => x.CategoryID == categoryId);
        }

        public void Add(Category category)
        {
            _context.Categories.Add(category);
        }

        public void Update(Category category)
        {
            _context.Categories.Update(category);
        }

        public void Remove(Category category)
        {
            _context.Categories.Remove(category);
        }
    }
}

using ECommerce.Domain.Entities;
using ECommerce.Domain.Ports;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Adapters;

public class CategoryRepository : ICategoryRepository
{
  private readonly AppDbContext _context;

  public CategoryRepository(AppDbContext context)
  {
    _context = context;
  }

  public IQueryable<Category> GetAllAsync()
  {
    return _context.Categories.AsQueryable();
  }

  public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await _context.Categories.FindAsync(new object[] { id }, cancellationToken);
  }

  public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
  {
    _context.Categories.Add(category);
    await _context.SaveChangesAsync(cancellationToken);
    return category;
  }

  public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
  {
    _context.Categories.Update(category);
    await _context.SaveChangesAsync(cancellationToken);
  }


  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var category = await _context.Categories.FindAsync(new object[] { id }, cancellationToken);
    if (category != null)
    {
      _context.Categories.Remove(category);
      await _context.SaveChangesAsync(cancellationToken);
    }
    return true;
  }
}
namespace ECommerce.Domain.Ports;

public interface ICategoryRepository
{
  Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  IQueryable<Category> GetAllAsync();
  Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);
  Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
  Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

}
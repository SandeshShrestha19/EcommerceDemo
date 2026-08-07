using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Ports;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<Product> GetAllAsync();
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignProductsToCategoryAsync(ICollection<Guid> productIds, Guid categoryId, CancellationToken cancellationToken = default);
}
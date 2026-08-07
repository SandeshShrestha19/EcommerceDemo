using ECommerce.Domain.Entities;
using ECommerce.Domain.Models;

public interface IProductFacade
{
  IQueryable<ProductResponseModel> GetAll(Guid? cursorId, int pageSize);
  Task<ProductResponseModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<Product> AddAsync(AddProductModel addModel, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, UpdateProductModel udpateModel, CancellationToken cancellationToken = default);
  Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
  Task IncreaseStockAsync(Guid id, int increasingQuantity, CancellationToken cancellationToken = default);
  Task DecreaseStockAsync(Guid id, int decreasingQuantity, CancellationToken cancellationToken = default);
}
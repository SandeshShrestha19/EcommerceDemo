using ECommerce.Domain.Models;

public interface ICategoryFacade
{
  IQueryable<CategoryResponseModel> GetAll(Guid? cursorId, int pageSize);
  Task<CategoryResponseModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<Category> AddAsync(AddCategoryModel addModel, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, UpdateCategoryModel udpateModel, CancellationToken cancellationToken = default);
  Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
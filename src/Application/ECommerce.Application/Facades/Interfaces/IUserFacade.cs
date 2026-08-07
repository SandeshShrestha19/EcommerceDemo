using Ecommerce.Domain.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Models;

public interface IUserFacade
{
  IQueryable<UserResponseModel> GetAll(Guid? cursorId, int pageSize);
  Task<UserResponseModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<User> AddAsync(AddUserModel addModel, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, UpdateUserModel udpateModel, CancellationToken cancellationToken = default);
  Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
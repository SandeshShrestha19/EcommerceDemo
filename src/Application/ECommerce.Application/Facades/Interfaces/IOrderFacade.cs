using Ecommerce.Domain.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Models;

public interface IOrderFacade
{
  IQueryable<OrderResponseModel> GetAll(Guid? cursorId, int pageSize, CurrentUser currentUser);
  Task<OrderResponseModel> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken = default);
  Task<Order> AddAsync(PlaceOrderModel addModel, CurrentUser currentUser, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, UpdateOrderModel udpateModel, CurrentUser currentUser, CancellationToken cancellationToken = default);
  Task<bool> DeleteAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken = default);
}

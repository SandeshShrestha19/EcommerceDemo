using Ecommerce.Domain.Models;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Models;
using ECommerce.Domain.Ports;
using Microsoft.Extensions.Logging;

public class OrderFacade : IOrderFacade
{
  private readonly IOrderRepository _orderRepository;
  private readonly IProductRepository _productRepository;
  private readonly IUserRepository _userRepository;
  private readonly ILogger<OrderFacade> _logger;
  private readonly IUnitOfWork _unitOfWork;

  public OrderFacade(IOrderRepository orderRepository, IProductRepository productRepository, IUserRepository userRepository, ILogger<OrderFacade> logger, IUnitOfWork unitOfWork)
  {
    _orderRepository = orderRepository;
    _productRepository = productRepository;
    _userRepository = userRepository;
    _logger = logger;
    _unitOfWork = unitOfWork;
  }

  public async Task<Order> AddAsync(PlaceOrderModel model, CurrentUser currentUser, CancellationToken cancellationToken = default)
  {
    if (model.Items == null || model.Items.Count == 0)
    {
      throw new ValidationException("Order must contain at least one item!");
    }

    Order order = null!;
    try
    {
      await _unitOfWork.ExecuteInTransactionAsync(async () =>
      {
        var user = await _userRepository.GetByIdAsync(currentUser.Id, cancellationToken)
            ?? throw NotFoundException.User();

        var orderItems = new List<OrderItem>();

        foreach (var item in model.Items)
        {
          if (item.Quantity <= 0)
          {
            throw new ValidationException("Item quantity must be greater than zero!");
          }

          var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken)
              ?? throw NotFoundException.Product();

          if (product.Stock < item.Quantity)
            throw new ValidationException("Product stock is less than order quantity!");

          product.Stock -= item.Quantity;
          await _productRepository.UpdateAsync(product, cancellationToken);

          orderItems.Add(new OrderItem
          {
            Id = Guid.CreateVersion7(),
            ProductId = product.Id,
            Quantity = item.Quantity,
            UnitPrice = product.Price
          });
        }

        var totalPrice = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

        order = new Order
        {
          Id = Guid.CreateVersion7(),
          UserId = currentUser.Id,
          OrderItems = orderItems,
          TotalPrice = totalPrice,
          OrderStatus = OrderStatus.Pending
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
      }, cancellationToken);

      _logger.LogInformation("Placing order for user: {UserId}", currentUser.Id);
      return order;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to place order!");
      throw;
    }
  }

  public async Task<bool> DeleteAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken = default)
  {
    try
    {
      var order = await _orderRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Order();
      EnsureCanAccess(order, currentUser);

      await _orderRepository.DeleteAsync(id, cancellationToken);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete order");
      throw new Exception($"Failed to delete order: {ex.Message}");
    }
  }

  public async Task UpdateAsync(Guid id, UpdateOrderModel model, CurrentUser currentUser, CancellationToken cancellationToken = default)
  {
    if (model.Items == null)
    {
      throw new ValidationException("Items cannot be null!");
    }

    try
    {
      await _unitOfWork.ExecuteInTransactionAsync(async () =>
      {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Order();
        EnsureCanAccess(order, currentUser);

        foreach (var item in model.Items)
        {
          if (item.Quantity < 0)
          {
            throw new ValidationException("Item quantity cannot be negative!");
          }

          var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken) ?? throw NotFoundException.Product();

          var existingItem = order.OrderItems
                      .FirstOrDefault(oi => oi.ProductId == item.ProductId);

          if (item.Quantity == 0)
          {
            if (existingItem != null)
            {
              product.Stock += existingItem.Quantity;
              await _productRepository.UpdateAsync(product, cancellationToken);
              order.OrderItems.Remove(existingItem);
            }
          }
          else if (existingItem != null)
          {
            int difference = item.Quantity - existingItem.Quantity;

            if (difference > 0 && product.Stock < difference)
            {
              throw new ValidationException($"Insufficient stock for {product.Name}!");
            }

            product.Stock -= difference;
            await _productRepository.UpdateAsync(product, cancellationToken);
            existingItem.Quantity = item.Quantity;
          }
          else
          {
            if (product.Stock < item.Quantity)
            {
              throw new ValidationException($"Insufficient stock for {product.Name}!");
            }

            product.Stock -= item.Quantity;
            await _productRepository.UpdateAsync(product, cancellationToken);

            order.OrderItems.Add(new OrderItem
            {
              Id = Guid.CreateVersion7(),
              OrderId = order.Id,
              ProductId = product.Id,
              Quantity = item.Quantity,
              UnitPrice = product.Price
            });
          }
        }

        order.TotalPrice = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

        if (model.OrderStatus.HasValue && model.OrderStatus.Value != order.OrderStatus)
        {
          EnsureCanManageOrderStatus(currentUser);
          EnsureValidStatusTransition(order.OrderStatus, model.OrderStatus.Value);
          order.OrderStatus = model.OrderStatus.Value;
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
      }, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogInformation(ex, "Error while updating order!");
      throw;
    }
  }

  public async Task UpdateOrderStatusAsync(Guid id, OrderStatus newStatus, CurrentUser currentUser, CancellationToken cancellationToken = default)
  {
    EnsureCanManageOrderStatus(currentUser);

    try
    {
      await _unitOfWork.ExecuteInTransactionAsync(async () =>
      {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Order();

        if (newStatus != order.OrderStatus)
        {
          EnsureValidStatusTransition(order.OrderStatus, newStatus);
          order.OrderStatus = newStatus;
          await _orderRepository.UpdateAsync(order, cancellationToken);
        }
      }, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update order status!");
      throw;
    }
  }

  public IQueryable<OrderResponseModel> GetAll(Guid? cursorId, int pageSize, CurrentUser currentUser)
  {
    var orders = _orderRepository.GetAllAsync();

    if (!currentUser.IsAdmin)
    {
      orders = orders.Where(o => o.UserId == currentUser.Id);
    }

    if (cursorId.HasValue)
    {
      orders = orders.Where(x => x.Id > cursorId.Value);
    }
    return orders.OrderBy(x => x.Id)
    .Take(pageSize)
    .Select(x => new OrderResponseModel
    {
      Id = x.Id,
      UserId = x.UserId,
      OrderItems = x.OrderItems,
      TotalPrice = x.TotalPrice,
      OrderDate = x.OrderDate,
      OrderStatus = x.OrderStatus
    });
  }

  public async Task<OrderResponseModel> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken = default)
  {
    try
    {
      var order = await _orderRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Order();
      EnsureCanAccess(order, currentUser);
      return ResponseMapper.ToOrderResponse(order);
    }
    catch (Exception ex)
    {
      _logger.LogInformation(ex, "Failed to retrieve data!");
      throw;
    }
  }

  private static void EnsureCanAccess(Order order, CurrentUser currentUser)
  {
    if (!currentUser.IsAdmin && order.UserId != currentUser.Id)
    {
      throw new ForbiddenException("You do not have access to this order!");
    }
  }

  // Only Admins/Managers may change an order's status; customers must not be
  // able to mark their own orders as Shipped/Delivered/Cancelled.
  private static void EnsureCanManageOrderStatus(CurrentUser currentUser)
  {
    var role = currentUser.Role;
    if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
    {
      throw new ForbiddenException("Only admins or managers can update order status!");
    }
  }

  private static void EnsureValidStatusTransition(OrderStatus current, OrderStatus next)
  {
    if (!OrderStatusTransition.CanTransition(current, next))
    {
      throw new BusinessException($"Cannot change order status from {current} to {next}!");
    }
  }
}

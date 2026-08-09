namespace ECommerce.Domain.Constants;

public enum OrderStatus
{
  Pending,
  Confirmed,
  Shipped,
  Delivered,
  Cancelled
}

/// <summary>
/// Defines which statuses an order may move to from its current status.
/// A status equal to the current one is always treated as a no-op (allowed).
/// </summary>
public static class OrderStatusTransition
{
  private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
  {
    [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
    [OrderStatus.Confirmed] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
    [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
    [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
    [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
  };

  public static bool CanTransition(OrderStatus current, OrderStatus next)
  {
    if (current == next)
    {
      return true;
    }
    return Allowed.TryGetValue(current, out var allowed) && allowed.Contains(next);
  }
}
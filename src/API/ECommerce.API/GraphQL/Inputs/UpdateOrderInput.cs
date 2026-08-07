using ECommerce.Domain.Constants;

public record UpdateOrderInput(
  List<OrderItemInput> Items,
  OrderStatus? OrderStatus
);

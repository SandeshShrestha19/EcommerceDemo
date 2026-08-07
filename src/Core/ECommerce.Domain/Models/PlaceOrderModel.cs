using ECommerce.Domain.Constants;

public class PlaceOrderModel
{
  public List<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();
}

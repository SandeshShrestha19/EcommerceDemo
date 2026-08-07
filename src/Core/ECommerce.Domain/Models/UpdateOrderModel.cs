
using ECommerce.Domain.Constants;

public class UpdateOrderModel
{
  public List<UpdateOrderItemModel> Items = new List<UpdateOrderItemModel>();
  public OrderStatus? OrderStatus { get; set; }
}
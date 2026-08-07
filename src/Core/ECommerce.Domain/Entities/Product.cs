using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class Product
{
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string Description { get; set; } = string.Empty;
  public int Stock { get; set; } = 0;
  public Guid CategoryId { get; set; }
  public Category? Category { get; set; }
  public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset ModifiedAt { get; set; }

  public void IncreaseStock(int increasingQuantity)
  {
    if (increasingQuantity <= 0)
    {
      throw new BusinessException("Quantity must be greater than 0.");
    }
    Stock += increasingQuantity;
  }
  public void DecreaseStock(int decreasingQuantity)
  {
    if (decreasingQuantity <= 0)
    {
      throw new BusinessException("Quantity must be greater than 0.");
    }
    if (Stock < decreasingQuantity)
    {
      throw new BusinessException("Insufficient stock.");
    }
    Stock -= decreasingQuantity;
  }

}
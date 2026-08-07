using ECommerce.Domain.Entities;

public class ProductImage
{
  public Guid Id { get; set; } = Guid.CreateVersion7();
  public Guid ProductId { get; set; }
  public Product? Product { get; set; }
  public string ImageUrl { get; set; } = string.Empty;
}
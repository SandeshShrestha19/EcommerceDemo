public class ProductResponseModel
{
  public Guid Id { get; set; }
  public string? Name { get; set; }
  public decimal Price { get; set; }
  public string? Description { get; set; }
  public int? Stock { get; set; }
  public Guid CategoryId { get; set; }
  public ICollection<ProductImage>? ProductImages { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

}
namespace ECommerce.Domain.Models;

public class UpdateProductModel
{
    public string? Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public Guid? CategoryId { get; set; }
    public int? Stock { get; set; }
    public ICollection<ProductImageModel>? ProductImages { get; set; } = new List<ProductImageModel>();
}

public record UpdateProductInput(
   string? Name,
   string? Description,
   int? Stock,
   decimal? Price,
   Guid? CategoryId,
   List<ProductImageModel>? ProductImages
);
public record ProductInput
(
   string Name,
   string Description,
   int Stock,
   decimal Price,
   Guid CategoryId,
   List<ProductImageModel> ProductImages
);
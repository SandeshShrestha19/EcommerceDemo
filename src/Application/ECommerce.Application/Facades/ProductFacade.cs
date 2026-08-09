using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Models;
using ECommerce.Domain.Ports;
using Microsoft.Extensions.Logging;

public class ProductFacade : IProductFacade
{
  private readonly IProductRepository _productRepository;
  private readonly ICategoryRepository _categoryRepository;
  private readonly IGeminiFacade _geminiFacade;
  private readonly ILogger<ProductFacade> _logger;
  private readonly IUnitOfWork _unitOfWork;

  public ProductFacade(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IGeminiFacade geminiFacade,
    ILogger<ProductFacade> logger,
    IUnitOfWork unitOfWork)
  {
    _productRepository = productRepository;
    _categoryRepository = categoryRepository;
    _geminiFacade = geminiFacade;
    _logger = logger;
    _unitOfWork = unitOfWork;
  }

  public async Task<Product> AddAsync(AddProductModel model, CancellationToken cancellationToken = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(model.Name))
      {
        throw new ValidationException("Product name is required");
      }

      if (model.Price <= 0)
      {
        throw new ValidationException("Price must be greater than 0");
      }

      if (model.Stock < 0)
      {
        throw new ValidationException("Stock cannot be negative");
      }

      // If no description was supplied, auto-generate one via Gemini.
      var description = model.Description;
      if (string.IsNullOrWhiteSpace(description))
      {
        description = await GenerateDescriptionAsync(model.Name, model.CategoryId, cancellationToken);
      }

      var product = new Product
      {
        Id = Guid.CreateVersion7(),
        Name = model.Name,
        Description = description,
        Price = model.Price,
        Stock = model.Stock,
        CategoryId = model.CategoryId,
        CreatedAt = DateTimeOffset.UtcNow,
        ModifiedAt = DateTimeOffset.UtcNow,
        ProductImages = model.ProductImages
        .Select(img => new ProductImage
        {
          ImageUrl = img.ImageUrl,
        })
        .ToList()
      };

      return await _productRepository.AddAsync(product, cancellationToken);

    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to add product");
      throw new Exception($"Failed to add product: {ex.Message}");
    }

  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    try
    {
      await _productRepository.DeleteAsync(id, cancellationToken);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete product");
      throw new Exception($"Failed to delete product: {ex.Message}");
    }

  }

  public IQueryable<ProductResponseModel> GetAll(Guid? cursorId, int pageSize)
  {
    var products = _productRepository.GetAllAsync();
    if (cursorId.HasValue)
    {
      products = products.Where(x => x.Id > cursorId.Value);
    }
    return products.OrderBy(x => x.Id)
    .Take(pageSize)
    .Select(x => new ProductResponseModel
    {
      Id = x.Id,
      Name = x.Name,
      Price = x.Price,
      Description = x.Description,
      Stock = x.Stock,
      CategoryId = x.CategoryId,
      ProductImages = x.ProductImages.Select(pi => new ProductImage
      {
        ImageUrl = pi.ImageUrl
      }).ToList(),
    });
  }

  public async Task<ProductResponseModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    try
    {
      var product = await _productRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Product();
      return ResponseMapper.ToProductResponse(product);
    }
    catch (Exception ex)
    {
      _logger.LogInformation(ex, "Failed to retrieve data!");
      throw;
    }
  }

  public async Task UpdateAsync(Guid id, UpdateProductModel updateModel, CancellationToken cancellationToken = default)
  {
    try
    {
      var product = await _productRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Product();

      product.Name = updateModel.Name ?? product.Name;
      if (updateModel.Description != null)
      {
        // An explicitly empty description means "regenerate it with Gemini".
        product.Description = string.IsNullOrWhiteSpace(updateModel.Description)
            ? await GenerateDescriptionAsync(product.Name, product.CategoryId, cancellationToken)
            : updateModel.Description;
      }
      if (updateModel.Price.HasValue)
      {
        if (updateModel.Price.Value <= 0)
        {
          throw new ValidationException("Price must be greater than 0");
        }
        product.Price = updateModel.Price.Value;
      }
      if (updateModel.Stock.HasValue)
      {
        if (updateModel.Stock.Value < 0)
        {
          throw new ValidationException("Stock cannot be negative");
        }
        product.Stock = updateModel.Stock.Value;
      }
      if (updateModel.CategoryId.HasValue)
      {
        product.CategoryId = updateModel.CategoryId.Value;
      }
      product.ModifiedAt = DateTimeOffset.UtcNow;

      await _productRepository.UpdateAsync(product, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update!");
      throw;
    }
  }

  public async Task IncreaseStockAsync(Guid productId, int increasingQuantity, CancellationToken cancellationToken = default)
  {
    var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
    product!.IncreaseStock(increasingQuantity);
    await _productRepository.UpdateAsync(product, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
  }

  public async Task DecreaseStockAsync(Guid productId, int decreasingQuantity, CancellationToken cancellationToken = default)
  {
    var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
    product!.DecreaseStock(decreasingQuantity);
    await _productRepository.UpdateAsync(product, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
  }

  // Builds a short marketing copy from the product name + category via Gemini.
  // A failure (missing API key, network, etc.) degrades gracefully: it logs a
  // warning and leaves the description empty instead of blocking the request.
  private async Task<string> GenerateDescriptionAsync(string productName, Guid categoryId, CancellationToken cancellationToken)
  {
    try
    {
      var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
      var prompt = $"""
        Generate a concise e-commerce product description.

        Product: {productName}
        Category: {category?.Name ?? "General"}

        Requirements:
        - 2 to 3 sentences
        - Professional tone
        - No exaggerated claims
        """;

      return await _geminiFacade.GenerateTextAsync(prompt, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Gemini description generation failed for product '{ProductName}'; using an empty description.", productName);
      return string.Empty;
    }
  }

}
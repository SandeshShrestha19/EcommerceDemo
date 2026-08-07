using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Models;
using ECommerce.Domain.Ports;
using Microsoft.Extensions.Logging;

public class CategoryFacade : ICategoryFacade
{
  private readonly IProductRepository _productRepository;
  private readonly ILogger<ProductFacade> _logger;
  private readonly ICategoryRepository _categoryRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CategoryFacade(IProductRepository productRepository, ILogger<ProductFacade> logger, IUnitOfWork unitOfWork, ICategoryRepository categoryRepository)
  {
    _productRepository = productRepository;
    _logger = logger;
    _unitOfWork = unitOfWork;
    _categoryRepository = categoryRepository;
  }

  public async Task<Category> AddAsync(AddCategoryModel model, CancellationToken cancellationToken = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(model.Name))
      {
        throw new ValidationException("Category name is required");
      }

      var category = new Category
      {
        Id = Guid.CreateVersion7(),
        Name = model.Name,
      };

      return await _categoryRepository.AddAsync(category, cancellationToken);

    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to add category");
      throw;
    }

  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    try
    {
      await _categoryRepository.DeleteAsync(id, cancellationToken);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete category");
      throw new Exception($"Failed to delete category: {ex.Message}");
    }

  }

  public IQueryable<CategoryResponseModel> GetAll(Guid? cursorId, int pageSize)
  {
    var categories = _categoryRepository.GetAllAsync();
    if (cursorId.HasValue)
    {
      categories = categories.Where(x => x.Id > cursorId.Value);
    }
    return categories.OrderBy(x => x.Id)
    .Take(pageSize)
    .Select(x => new CategoryResponseModel
    {
      Id = x.Id,
      Name = x.Name,
      Products = x.Products,
      CreatedAt = x.CreatedAt,
      ModifiedAt = x.ModifiedAt,
    });
  }

  public async Task<CategoryResponseModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    try
    {
      var category = await _categoryRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Category();
      return ResponseMapper.ToCategoryResponse(category);
    }
    catch (Exception ex)
    {
      _logger.LogInformation(ex, "Failed to retrieve data!");
      throw;
    }
  }

  public async Task UpdateAsync(Guid id, UpdateCategoryModel updateModel, CancellationToken cancellationToken = default)
  {
    try
    {
      await _unitOfWork.ExecuteInTransactionAsync(async () =>
      {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.Category();

        if (!string.IsNullOrWhiteSpace(updateModel.Name))
        {
          category.Name = updateModel.Name;
        }

        category.ModifiedAt = DateTimeOffset.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        if (updateModel.ProductIds is not null)
        {
          await _productRepository.AssignProductsToCategoryAsync(
              updateModel.ProductIds,
              category.Id,
              cancellationToken);
        }
      }, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update category!");
      throw;
    }
  }
}
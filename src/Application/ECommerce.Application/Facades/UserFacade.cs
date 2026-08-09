using Ecommerce.Domain.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Ports;
using Microsoft.Extensions.Logging;

public class UserFacade : IUserFacade
{
  private readonly IUserRepository _userRepository;
  private readonly ILogger<UserFacade> _logger;
  private readonly IUnitOfWork _unitOfWork;

  public UserFacade(IUserRepository userRepository, ILogger<UserFacade> logger, IUnitOfWork unitOfWork)
  {
    _userRepository = userRepository;
    _logger = logger;
    _unitOfWork = unitOfWork;
  }

  public async Task<User> AddAsync(AddUserModel model, CancellationToken cancellationToken = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(model.Name))
      {
        throw new ValidationException("Name can't be empty!");
      }

      if (string.IsNullOrWhiteSpace(model.Username))
      {
        throw new ValidationException("Username can't be empty!");
      }

      if (!model.Email.Contains('@'))
      {
        throw new ValidationException("Email should contain '@'!");
      }

      if (!model.Password.Any(ch => "!@#$%^&*()_+-=[]{}|;:',.<>?/`~".Contains(ch)))
      {
        throw new ValidationException("Password should contain at least one special character!");
      }

      if (model.Password!.Length < 8)
      {
        throw new ValidationException("Password should be at least 8 characters long!");
      }

      var existingEmail = await _userRepository.GetByEmailAsync(model.Email, cancellationToken);
      if (existingEmail != null)
      {
        throw ConflictException.EmailAlreadyExists();
      }
      var user = new User
      {
        Id = Guid.CreateVersion7(),
        Name = model.Name,
        Email = Convert.ToString(model.Email).ToLower(),
        Role = "User",
        Password = PasswordHashHandler.HashPassword(model.Password!),
        CreatedAt = DateTime.UtcNow,
        IsActive = true,
        IsLoggedIn = false,
        Username = model.Username,
        PhoneNumber = model.PhoneNumber ?? string.Empty,
        TwoFactorEnabled = false
      };
      return await _userRepository.AddAsync(user, cancellationToken);

    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to add user!");
      throw;
    }
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    try
    {
      await _userRepository.DeleteAsync(id, cancellationToken);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete product");
      throw new Exception($"Failed to delete product: {ex.Message}");
    }

  }

  public IQueryable<UserResponseModel> GetAll(Guid? cursorId, int pageSize)
  {
    var users = _userRepository.GetAllAsync();
    if (cursorId.HasValue)
    {
      users = users.Where(x => x.Id > cursorId.Value);
    }
    return users.OrderBy(x => x.Id)
    .Take(pageSize)
    .Select(x => new UserResponseModel
    {
      Id = x.Id,
      Name = x.Name,
      Email = x.Email,
      Role = x.Role,
      IsLoggedIn = x.IsLoggedIn,
      IsActive = x.IsActive,
      CreatedAt = x.CreatedAt,
      Orders = x.Orders,
      PhoneNumber = x.PhoneNumber,
      Username = x.Username,
    });
  }

  public async Task UpdateAsync(Guid id, UpdateUserModel model, CancellationToken cancellationToken = default)
  {
    try
    {
      var user = await _userRepository.GetByIdAsync(id, cancellationToken)
          ?? throw NotFoundException.User();

      if (!string.IsNullOrWhiteSpace(model.Name))
      {
        user.Name = model.Name.Trim();
      }

      if (!string.IsNullOrWhiteSpace(model.Email))
      {
        try
        {
          _ = new System.Net.Mail.MailAddress(model.Email);
        }
        catch
        {
          throw new ValidationException("Invalid email address!");
        }

        var existingUser = await _userRepository.GetByEmailAsync(
            model.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        if (existingUser != null && existingUser.Id != id)
        {
          throw ConflictException.EmailAlreadyExists();
        }

        user.Email = model.Email.Trim().ToLowerInvariant();
      }

      if (!string.IsNullOrWhiteSpace(model.Username))
      {
        user.Username = model.Username.Trim();
      }

      if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
      {
        user.PhoneNumber = model.PhoneNumber.Trim();
      }

      if (!string.IsNullOrWhiteSpace(model.Password))
      {
        if (model.Password.Length < 8)
        {
          throw new ValidationException("Password should be at least 8 characters long!");
        }

        if (!model.Password.Any(ch => "!@#$%^&*()_+-=[]{}|;:',.<>?/`~".Contains(ch)))
        {
          throw new ValidationException("Password should contain at least one special character!");
        }

        user.Password = PasswordHashHandler.HashPassword(model.Password);
      }

      await _userRepository.UpdateAsync(user, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update user details!");
      throw;
    }
  }

  public async Task<UserResponseModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    try
    {
      var user = await _userRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.User();
      return ResponseMapper.ToUserResponse(user);
    }
    catch (Exception ex)
    {
      _logger.LogInformation(ex, "Failed to retrieve data!");
      throw;
    }
  }
}
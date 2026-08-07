using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Ports;
using Microsoft.Extensions.Logging;

public class SetUserActiveStatusUseCase : ISetUserActiveStatusUseCase
{
  private readonly IUserRepository _userRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<SetUserActiveStatusUseCase> _logger;

  public SetUserActiveStatusUseCase(IUserRepository userRepository, ILogger<SetUserActiveStatusUseCase> logger, IUnitOfWork unitOfWork)
  {
    _userRepository = userRepository;
    _logger = logger;
    _unitOfWork = unitOfWork;
  }

  public async Task ExecuteAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
  {
    try
    {
      var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw NotFoundException.User();
      user.IsActive = isActive;
      await _userRepository.UpdateAsync(user, cancellationToken);
      await _unitOfWork.SaveChangesAsync(cancellationToken);
      _logger.LogInformation($"User {user.Email} IsActive set to {isActive}");
    }
    catch(Exception ex)
    {
      _logger.LogInformation(ex, "Failed to update the status!");
      throw;
    }
  }
}
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Ports;

namespace ECommerce.Application.UseCase;

public class Verify2FAUseCase : IVerify2FAUseCase
{
  private readonly IUserRepository _userRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly TwoFactorService _twoFactorService;

  public Verify2FAUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, TwoFactorService twoFactorService)
  {
    _userRepository = userRepository;
    _unitOfWork = unitOfWork;
    _twoFactorService = twoFactorService;
  }

  public async Task<bool> ExecuteAsync(Guid userId, string code, CancellationToken cancellationToken = default)
  {
    var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw NotFoundException.User();

    if (string.IsNullOrEmpty(user.TwoFactorSecret))
    {
      throw new ValidationException("2FA is not set up!");
    }
    var isValid = _twoFactorService.VerifyCode(user.TwoFactorSecret, code);
    if (!isValid)
    {
      throw new UnauthorizedException("Invalid 2FA code!");
    }
    user.TwoFactorEnabled = true;
    await _userRepository.UpdateAsync(user, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    return true;
  }
}
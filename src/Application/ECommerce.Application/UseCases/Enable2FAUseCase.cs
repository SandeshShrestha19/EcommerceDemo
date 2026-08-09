using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Ports;

namespace ECommerce.Application.UseCase;

public class Enable2FAUseCase : IEnable2FAUseCase
{
  private readonly IUserRepository _userRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly TwoFactorService _twoFactorService;
  public Enable2FAUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, TwoFactorService twoFactorService)
  {
    _userRepository = userRepository;
    _unitOfWork = unitOfWork;
    _twoFactorService = twoFactorService;
  }

  public async Task<Enable2FAResponseModel> ExecuteAsync(Guid id, string password, CancellationToken cancellationToken = default)
  {
    var user = await _userRepository.GetByIdAsync(id, cancellationToken) ?? throw new Exception($"User with Id: {id} not found!");

    // Enabling 2FA must be confirmed with the account password. Google-only
    // accounts hold a random internal password, so they are allowed to skip it.
    if (!user.IsGoogleUser && !PasswordHashHandler.VerifyPassword(password, user.Password))
    {
      throw new UnauthorizedException("Password is incorrect!");
    }

    var secretKey = _twoFactorService.GenerateSecretKey() ?? throw new Exception("Failed to generate secret key!");

    var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email, secretKey) ?? throw new Exception("Failed to generate Qr code Uri!");

    var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri) ?? throw new Exception("Failed to generate Qr code image");

    user.TwoFactorSecret = secretKey;
    // Confirming with the account password is enough to enable 2FA; the user
    // scans the QR and enters the code the next time they log in.
    user.TwoFactorEnabled = true;
    await _userRepository.UpdateAsync(user, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return new Enable2FAResponseModel
    {
      SecretKey = secretKey,
      QrCodeUri = qrCodeUri,
      QrCodeImage = qrCodeImage
    };

  }
}
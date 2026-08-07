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

  public async Task<Enable2FAResponseModel> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var user = await _userRepository.GetByIdAsync(id, cancellationToken) ?? throw new Exception($"User with Id: {id} not found!");

    var secretKey = _twoFactorService.GenerateSecretKey() ?? throw new Exception("Failed to generate secret key!");

    var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email, secretKey) ?? throw new Exception("Failed to generate Qr code Uri!");

    var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri) ?? throw new Exception("Failed to generate Qr code image");

    user.TwoFactorSecret = secretKey;
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
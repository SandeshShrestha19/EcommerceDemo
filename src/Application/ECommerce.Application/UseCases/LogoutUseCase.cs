using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Ports;
using Microsoft.Extensions.Logging;

public class LogoutUseCase : ILogoutUseCase
{
  private readonly IUserRepository _userRepository;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly IBlacklistedTokenRepository _blacklistedTokenRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<LogoutUseCase> _logger;

  public LogoutUseCase(IUserRepository userRepository, ILogger<LogoutUseCase> logger, IUnitOfWork unitOfWork, IRefreshTokenRepository refreshTokenRepository, IBlacklistedTokenRepository blacklistedTokenRepository)
  {
    _userRepository = userRepository;
    _logger = logger;
    _unitOfWork = unitOfWork;
    _refreshTokenRepository = refreshTokenRepository;
    _blacklistedTokenRepository = blacklistedTokenRepository;
  }

  public async Task ExecuteAsync(Guid id, string refreshToken, string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
  {
    try
    {
      var user = await _userRepository.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.User();

      user.IsLoggedIn = false;
      await _userRepository.UpdateAsync(user, cancellationToken);

      var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken) ?? throw NotFoundException.RefreshToken();

      storedToken.IsRevoked = true;
      await _refreshTokenRepository.UpdateASync(storedToken, cancellationToken);

      await _blacklistedTokenRepository.AddAsync(new BlacklistedToken
      {
        Id = Guid.CreateVersion7(),
        Jti = jti,
        ExpiresAt = expiresAt,
        CreatedAt = DateTime.UtcNow
      }, cancellationToken);

      await _unitOfWork.SaveChangesAsync(cancellationToken);

    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to log out!");
      throw;
    }
  }
}
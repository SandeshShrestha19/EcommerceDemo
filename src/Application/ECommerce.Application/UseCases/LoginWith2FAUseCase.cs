using System.Security.Cryptography;
using ECommerce.Applcation.Helpers;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Models;
using ECommerce.Domain.Ports;
using ECommerce.Domain.UseCase;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.UseCases;

public class LoginWith2FAUseCase : ILoginWith2FAUseCase
{
  private readonly IUserRepository _userRepository;
  private readonly JwtTokenGenerator _jwtTokenGenerator;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<LoginWith2FAUseCase> _logger;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly TwoFactorService _twoFactorService;
  public LoginWith2FAUseCase(IUserRepository userRepository, JwtTokenGenerator jwtTokenGenerator, IUnitOfWork unitOfWork, IRefreshTokenRepository refreshTokenRepository, TwoFactorService twoFactorService, ILogger<LoginWith2FAUseCase> logger)
  {
    _userRepository = userRepository;
    _jwtTokenGenerator = jwtTokenGenerator;
    _unitOfWork = unitOfWork;
    _refreshTokenRepository = refreshTokenRepository;
    _twoFactorService = twoFactorService;
    _logger = logger;
  }

  public async Task<LoginResponseModel> ExecuteAsync(string tempToken, string code, CancellationToken cancellationToken = default)
  {
    try
    {
      var userId = _jwtTokenGenerator.ValidateTempToken(tempToken);

      var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw NotFoundException.User();

      if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
      {
        throw new BusinessException("2FA not set up!");
      }
      var isValid = _twoFactorService.VerifyCode(user.TwoFactorSecret, code);
      if (!isValid)
      {
        throw new ValidationException("Invalid code!");
      }

      var token = _jwtTokenGenerator.GenerateAccessToken(user);

      var refreshToken = new RefreshToken
      {
        Id = Guid.CreateVersion7(),
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        UserId = user.Id,
        CreatedAt = DateTime.UtcNow,
        ExpiresIn = DateTime.UtcNow.AddDays(7),
        IsRevoked = false
      };
      user.IsLoggedIn = true;
      await _userRepository.UpdateAsync(user, cancellationToken);
      await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
      await _unitOfWork.SaveChangesAsync(cancellationToken);

      return new LoginResponseModel
      {
        RequiresTwoFactor = false,
        EmailOrUsername = (user.Email is not null) ? user.Email : user.Username,
        RefreshToken = refreshToken.Token,
        AccessToken = token,
        ExpiresIn = 7,
        Message = "Login successful!"
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to login!");
      throw;
    }
  }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using ECommerce.Applcation.Helpers;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Models;
using ECommerce.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.UseCases;

public class LoginUseCase : ILoginUseCase
{
  private readonly IUserRepository _userRepository;
  private readonly JwtTokenGenerator _jwtTokenGenerator;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<LoginUseCase> _logger;

  public LoginUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<LoginUseCase> logger, IRefreshTokenRepository refreshTokenRepository, JwtTokenGenerator jwtTokenGenerator)
  {
    _userRepository = userRepository;
    _logger = logger;
    _unitOfWork = unitOfWork;
    _refreshTokenRepository = refreshTokenRepository;
    _jwtTokenGenerator = jwtTokenGenerator;
  }

  public async Task<LoginResponseModel> ExecuteAsync(LoginModel model, CancellationToken cancellationToken = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(model.EmailOrUsername) || string.IsNullOrWhiteSpace(model.Password))
      {
        throw new ValidationException("Email or Password field is empty!");
      }

      //verify user by email or username
      var user = _userRepository.GetAllAsync().FirstOrDefault(u => u.Email == model.EmailOrUsername || u.Username == model.EmailOrUsername) ?? throw new UnauthorizedException();

      var isValid = PasswordHashHandler.VerifyPassword(model.Password, user.Password);
      if (!isValid)
      {
        throw new UnauthorizedException();
      }
      if (!user.IsActive)
      {
        throw BusinessException.AccountDisabled();
      }

      var tempToken = _jwtTokenGenerator.GenerateTempToken(user);
      if (user.TwoFactorEnabled)
      {
        return new LoginResponseModel
        {
          RequiresTwoFactor = true,
          EmailOrUsername = model.EmailOrUsername,
          TempToken = tempToken
        };
      }

      user.IsLoggedIn = true;

      var token = _jwtTokenGenerator.GenerateAccessToken(user);

      var refreshToken = new RefreshToken
      {
        Id = Guid.CreateVersion7(),
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        UserId = user.Id,
        ExpiresIn = DateTime.UtcNow.AddDays(7),
        IsRevoked = false
      };

      await _userRepository.UpdateAsync(user, cancellationToken);
      await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
      await _unitOfWork.SaveChangesAsync(cancellationToken);

      return new LoginResponseModel
      {
        RequiresTwoFactor = false,
        EmailOrUsername = model.EmailOrUsername,
        ExpiresIn = 7,
        AccessToken = token,
        Message = "Login Successful!",
        RefreshToken = refreshToken.Token
      };

    }
    catch (Exception ex)
    {
      _logger.LogInformation(ex, "Failed to login!");
      throw;
    }

  }
}
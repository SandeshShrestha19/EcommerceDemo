using ECommerce.Applcation.Helpers;
using ECommerce.Application.Helpers;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Models;
using ECommerce.Domain.Ports;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ECommerce.Application.UseCases;

public class GoogleAuthUseCase : IGoogleAuthUseCase
{
  private readonly IUserRepository _userRepository;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly GoogleAuthService _googleAuthService;
  private readonly JwtTokenGenerator _jwtTokenGenerator;
  private readonly ILogger<GoogleAuthUseCase> _logger;

  public GoogleAuthUseCase(
      IUserRepository userRepository,
      IRefreshTokenRepository refreshTokenRepository,
      IUnitOfWork unitOfWork,
      GoogleAuthService googleAuthService,
      JwtTokenGenerator jwtTokenGenerator,
      ILogger<GoogleAuthUseCase> logger)
  {
    _userRepository = userRepository;
    _refreshTokenRepository = refreshTokenRepository;
    _unitOfWork = unitOfWork;
    _googleAuthService = googleAuthService;
    _jwtTokenGenerator = jwtTokenGenerator;
    _logger = logger;
  }

  public async Task<LoginResponseModel> ExecuteAsync(string idToken)
  {
    try
    {
      var payload = await GetVerifiedPayloadAsync(idToken);

      var user = _userRepository.GetAllAsync()
          .FirstOrDefault(u => u.Email == payload.Email);

      if (user == null)
      {
        user = new User
        {
          Id = Guid.NewGuid(),
          Name = payload.Name,
          Email = payload.Email,
          GoogleId = payload.Subject,
          ProfilePicture = payload.Picture,
          IsGoogleUser = true,
          Role = "User",
          Password = PasswordHashHandler.HashPassword(
                Guid.NewGuid().ToString()), // random password
          IsActive = true,
          IsLoggedIn = true,
          CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
      }
      else
      {
        // update existing user
        user.GoogleId = payload.Subject;
        user.ProfilePicture = payload.Picture;
        user.IsLoggedIn = true;
        await _userRepository.UpdateAsync(user);
      }

      // Step 3 - check if user is active
      if (!user.IsActive)
        throw BusinessException.AccountDisabled();

      // Step 4 - generate tokens
      var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
      var refreshToken = new RefreshToken
      {
        Id = Guid.NewGuid(),
        Token = Convert.ToBase64String(
              RandomNumberGenerator.GetBytes(64)),
        UserId = user.Id,
        ExpiresIn = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow,
        IsRevoked = false
      };

      await _refreshTokenRepository.AddAsync(refreshToken);
      await _unitOfWork.SaveChangesAsync();

      return new LoginResponseModel
      {
        EmailOrUsername = (user.Email is not null) ? user.Email : user.Username,
        AccessToken = accessToken,
        RefreshToken = refreshToken.Token,
        ExpiresIn = 7,
        Message = "Google login successful!"
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Google auth failed!");
      throw;
    }
  }

  private async Task<GoogleJsonWebSignature.Payload> GetVerifiedPayloadAsync(string idToken)
  {
    try
    {
      return await _googleAuthService.VerifyGoogleToken(idToken);
    }
    catch (Exception ex)
    {
      _logger.LogInformation(ex, "Google token validation failed!");
      throw new UnauthorizedException("Invalid Google ID token!");
    }
  }
}
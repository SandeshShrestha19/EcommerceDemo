using ECommerce.Domain.Exceptions;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Application.Helpers;

public class GoogleAuthService
{
  private readonly IConfiguration _configuration;


  public GoogleAuthService(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
  {
    var settings = new GoogleJsonWebSignature.ValidationSettings
    {
      Audience = new[] { _configuration["GoogleOAuth:ClientId"] }
    };

    try
    {
      var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
      return payload;
    }
    catch (Exception Exception)
    {
      Console.WriteLine(Exception);
      throw;
    }
  }
}
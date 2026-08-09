using HotChocolate;

namespace ECommerce.Domain.Models;

public class LoginResponseModel
{
  public string EmailOrUsername { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public int ExpiresIn { get; set; }
  public string TempToken { get; set; } = string.Empty;
  public bool RequiresTwoFactor { get; set; } = false;
  // Present only when RequiresTwoFactor is true, so the login screen can show
  // the QR for the authenticator app together with the code prompt.
  public string? QrCodeImage { get; set; }
  public string? SecretKey { get; set; }
  public string? AccessToken { get; set; } = string.Empty;
  [GraphQLIgnore]
  public string RefreshToken { get; set; } = string.Empty;
}
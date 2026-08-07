using HotChocolate;

namespace ECommerce.Domain.Models;

public class LoginResponseModel
{
  public string EmailOrUsername { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public int ExpiresIn { get; set; }
  public string TempToken { get; set; } = string.Empty;
  public bool RequiresTwoFactor { get; set; } = false;
  public string? AccessToken { get; set; } = string.Empty;
  [GraphQLIgnore]
  public string RefreshToken { get; set; } = string.Empty;
}
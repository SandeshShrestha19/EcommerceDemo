namespace ECommerce.Domain.Models;

public class LoginModel
{
  public string EmailOrUsername { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;

}
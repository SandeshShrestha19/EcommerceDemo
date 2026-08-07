namespace ECommerce.Domain.Models;

public record CurrentUser(Guid Id, string Role)
{
  public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
}

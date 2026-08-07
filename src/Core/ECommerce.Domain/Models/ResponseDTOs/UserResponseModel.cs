using ECommerce.Domain.Entities;

public class UserResponseModel
{
  public Guid Id { get; set; }
  public string? Name { get; set; }
  public string? Email { get; set; }
  public bool IsLoggedIn { get; set; }
  public bool IsActive { get; set; }
  public string? Username { get; set; }
  public string? PhoneNumber { get; set; }
  public string? CompanyName { get; set; }
  public string? Role { get; set; }
  public DateTime CreatedAt { get; set; }
  public ICollection<Order> Orders { get; set; } = new List<Order>();
}
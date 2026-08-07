
namespace ECommerce.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsLoggedIn { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Password { get; set; } = default!;
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? GoogleId { get; set; }
    public string? ProfilePicture { get; set; }
    public bool IsGoogleUser { get; set; } = false;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

}
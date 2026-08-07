using ECommerce.Domain.Models;

namespace ECommerce.Domain.Ports;
public interface ILogoutUseCase
{
  Task ExecuteAsync(Guid id, string refreshToken, string jti, DateTime tokenExpiry, CancellationToken cancellationToken = default);
}
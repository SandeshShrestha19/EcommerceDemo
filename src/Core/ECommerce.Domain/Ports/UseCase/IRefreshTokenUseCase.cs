using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Ports;

public interface IRefreshTokenUseCase
{
  Task<string> ExecuteAsync(string refreshToken, CancellationToken cancellationToken = default);
}
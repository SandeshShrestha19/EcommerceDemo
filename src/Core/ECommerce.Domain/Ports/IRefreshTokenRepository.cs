using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Ports;

public interface IRefreshTokenRepository
{
  IQueryable<RefreshToken> GetAll();
  Task<RefreshToken?> GetByTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
  Task<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
  Task UpdateASync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
  // Task<bool> DeleteAsync(Guid id);

}
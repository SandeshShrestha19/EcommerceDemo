namespace ECommerce.Domain.Ports;

public interface IBlacklistedTokenRepository
{
  Task AddAsync(BlacklistedToken blackListedToken, CancellationToken cancellationToken = default);
  Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
  Task DeleteExpiredAsync(CancellationToken cancellationToken = default);

}
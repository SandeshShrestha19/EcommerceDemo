using ECommerce.Domain.Ports;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BlacklistedTokenRepository : IBlacklistedTokenRepository
{
  private readonly AppDbContext _context;
  public BlacklistedTokenRepository(AppDbContext context)
  {
    _context = context;
  }
  public async Task AddAsync(BlacklistedToken blacklistedToken, CancellationToken cancellationToken = default)
  {
    _context.BlacklistedTokens.Add(blacklistedToken);
    await _context.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
  {
    var blacklistedTokens = await _context.BlacklistedTokens.Where(x => x.ExpiresAt < DateTime.UtcNow).ToListAsync(cancellationToken);
    _context.BlacklistedTokens.RemoveRange(blacklistedTokens);
    await _context.SaveChangesAsync(cancellationToken);
  }

  public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
  {
    return await _context.BlacklistedTokens.AnyAsync(x => x.Jti == jti, cancellationToken);
  }
}
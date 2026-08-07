using ECommerce.Domain.Ports;
using Microsoft.Extensions.Logging;

public class BlacklistedTokenUseCase : IBlacklistedTokenUseCase
{
  private readonly IBlacklistedTokenRepository _blacklistedTokenRepository;
  private readonly ILogger<BlacklistedTokenUseCase> _logger;

  public BlacklistedTokenUseCase(IBlacklistedTokenRepository blacklistedTokenRepository, ILogger<BlacklistedTokenUseCase> logger){
    _blacklistedTokenRepository = blacklistedTokenRepository;
    _logger = logger;
  }

  public async Task ExecuteAsync(AddBlacklistedTokenModel addModel, CancellationToken cancellationToken = default)
  {
    try
    {
      var jtiToken = new BlacklistedToken
      {
        Id = Guid.CreateVersion7(),
        Jti = addModel.Jti,
        ExpiresAt = addModel.ExpiresAt,
        CreatedAt = DateTime.UtcNow
      };
      await _blacklistedTokenRepository.AddAsync(jtiToken, cancellationToken);
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Failed to create jti token!");
      throw;
    }
  }
}
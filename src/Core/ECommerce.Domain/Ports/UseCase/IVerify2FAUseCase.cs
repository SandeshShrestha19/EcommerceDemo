public interface IVerify2FAUseCase
{
  Task<bool> ExecuteAsync(Guid id, string code, CancellationToken cancellationToken = default);
}
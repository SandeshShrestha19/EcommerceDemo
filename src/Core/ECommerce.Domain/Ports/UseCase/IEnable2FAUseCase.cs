public interface IEnable2FAUseCase
{
  Task<Enable2FAResponseModel> ExecuteAsync(Guid id, string password, CancellationToken cancellationToken = default);
}
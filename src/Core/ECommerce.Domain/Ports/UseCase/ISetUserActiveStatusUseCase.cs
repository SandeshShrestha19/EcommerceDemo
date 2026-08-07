public interface ISetUserActiveStatusUseCase
{
  Task ExecuteAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
}
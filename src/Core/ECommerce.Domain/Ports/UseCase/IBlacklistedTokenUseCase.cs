public interface IBlacklistedTokenUseCase
{
  Task ExecuteAsync(AddBlacklistedTokenModel addModel, CancellationToken cancellationToken = default);
}
public interface IGeminiFacade
{
  Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
}
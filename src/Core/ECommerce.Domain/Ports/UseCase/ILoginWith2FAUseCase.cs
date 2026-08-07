using ECommerce.Domain.Models;

namespace ECommerce.Domain.UseCase;
public interface ILoginWith2FAUseCase
{
  Task<LoginResponseModel> ExecuteAsync(string email, string code, CancellationToken cancellationToken = default);
}
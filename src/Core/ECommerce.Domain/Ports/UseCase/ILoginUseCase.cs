using ECommerce.Domain.Models;

namespace ECommerce.Domain.Ports;
public interface ILoginUseCase
{
  Task<LoginResponseModel> ExecuteAsync(LoginModel model, CancellationToken cancellationToken = default);
}
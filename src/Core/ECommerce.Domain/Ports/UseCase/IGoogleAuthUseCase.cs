// Domain/Ports/IGoogleAuthUseCase.cs
using ECommerce.Domain.Models;

namespace ECommerce.Domain.Ports;

public interface IGoogleAuthUseCase
{
  Task<LoginResponseModel> ExecuteAsync(string idToken);
}
using ECommerce.Applcation.Helpers;
using ECommerce.Application.Helpers;
using ECommerce.Application.UseCase;
using ECommerce.Application.UseCases;
using ECommerce.Domain.Ports;
using ECommerce.Domain.UseCase;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
  public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
  {
    services.AddScoped<ILoginUseCase, LoginUseCase>();
    services.AddScoped<ILoginWith2FAUseCase, LoginWith2FAUseCase>();
    services.AddScoped<ILogoutUseCase, LogoutUseCase>();
    services.AddScoped<IEnable2FAUseCase, Enable2FAUseCase>();
    services.AddScoped<IVerify2FAUseCase, Verify2FAUseCase>();

    services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
    services.AddScoped<ISetUserActiveStatusUseCase, SetUserActiveStatusUseCase>();
    services.AddScoped<JwtTokenGenerator>();
    services.AddScoped<TwoFactorService>();
    services.AddScoped<IBlacklistedTokenUseCase, BlacklistedTokenUseCase>();

    services.AddScoped<IUserFacade, UserFacade>();
    services.AddScoped<IProductFacade, ProductFacade>();
    services.AddScoped<IOrderFacade, OrderFacade>();
    services.AddScoped<ICategoryFacade, CategoryFacade>();
    services.AddScoped<GoogleAuthService>();
    services.AddScoped<IGoogleAuthUseCase, GoogleAuthUseCase>();
    services.AddHttpClient<IGeminiFacade, GeminiFacade>();

    return services;
  }
}
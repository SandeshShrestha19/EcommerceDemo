using ECommerce.Domain.Ports;
using ECommerce.Infrastructure.Adapters;
using ECommerce.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastrutureDependencies(this IServiceCollection services)
  {
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    services.AddScoped<IBlacklistedTokenRepository, BlacklistedTokenRepository>();
    services.AddScoped<ICategoryRepository, CategoryRepository>();
    services.AddHostedService<UserActivityJob>();
    return services;
  }
}
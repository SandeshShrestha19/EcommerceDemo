public static class AuthorizationExtension
{
  public static IServiceCollection AddAuthorizationDependencies(
    this IServiceCollection services)
  {
    services.AddAuthorization(options =>
    {
      options.AddPolicy("ActiveUser", policy =>
      {
        policy.RequireAuthenticatedUser();

        policy.RequireAssertion(context =>
          {
            var value = context.User
                  .FindFirst("IsActive")
                  ?.Value;

            return bool.TryParse(
                         value,
                         out var isActive)
                     && isActive;
          });
      });
    });

    return services;
  }
}
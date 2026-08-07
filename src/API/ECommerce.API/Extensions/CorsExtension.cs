public static class CorsExtension
{
  public static IServiceCollection AddCorsDependencies(this IServiceCollection services, IConfiguration configuration)
  {
    var allowedOrigins = configuration["AllowedOrigins"]
        ?? "http://localhost:3000,http://localhost:5173";

    services.AddCors(options =>
    {
      options.AddPolicy("AllowFrontend", policy =>
          policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });
    return services;
  }
}
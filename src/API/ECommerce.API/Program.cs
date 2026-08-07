using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ECommerce.Domain.Ports;
using ECommerce.Domain.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthenticationDependencies(builder.Configuration);
builder.Services.AddAuthorizationDependencies();
builder.Services.AddInfrastrutureDependencies();
builder.Services.AddApplicationDependencies();
builder.Services.AddLogging();
builder.Services.AddScoped<ResponseMapper>();
builder.Services.AddGraphQLDependencies();
builder.Services.AddCorsDependencies(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<GeminiOptionsModel>(
    builder.Configuration.GetSection("Gemini"));
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure();
        });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    db.Database.Migrate();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowFrontend");
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (jti != null)
        {
            var blacklistedRepo = context.RequestServices.GetRequiredService<IBlacklistedTokenRepository>();

            var isBlacklisted = await blacklistedRepo.IsBlacklistedAsync(jti);

            if (isBlacklisted)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Token has been revoked! Please try again!"
                });
                return;
            }
        }


        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId != null)
        {
            var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users.FindAsync(Guid.Parse(userId));

            if (user != null && !user.IsActive)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Your account has been disabled! Contact admin."
                });
                return;
            }
        }
    }
    await next();
});

app.MapGraphQL();

app.Run();
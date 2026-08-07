using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class AuthenticationExtension
{
    public static IServiceCollection AddAuthenticationDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; //make it true for production when you want https
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = configuration["JwtConfig:Issuer"], //who created the token
                ValidAudience = configuration["JwtConfig:Audience"], //who used the token
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtConfig:Key"]!)), //secret key to verify token
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true, //check if token is expired
                ValidateIssuerSigningKey = true
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var request = context.Request;

                    // A 2FA temp token must never authenticate a session — from
                    // the cookie OR the Authorization header. It is only ever
                    // valid via the tempToken argument of the loginWith2FA
                    // mutation. Detect it by its "2fa_pending" claim and reject.
                    var candidate = context.Token;
                    if (string.IsNullOrEmpty(candidate))
                    {
                        var header = request.Headers.Authorization.ToString();
                        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            candidate = header["Bearer ".Length..].Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(candidate) && IsTempToken(candidate))
                    {
                        context.Fail("2FA verification is required before accessing this resource.");
                        return Task.CompletedTask;
                    }

                    // Only the real access token cookie authenticates a session.
                    if (request.Cookies.TryGetValue(
                            "token",
                            out var accessToken))
                    {
                        // A 2FA temp token must never authenticate a session,
                        // even when supplied via the cookie.
                        if (IsTempToken(accessToken))
                        {
                            context.Fail("2FA verification is required before accessing this resource.");
                            return Task.CompletedTask;
                        }

                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });
        return services;
    }

    private static bool IsTempToken(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.Claims.Any(c => c.Type == "2fa_pending" && c.Value == "true");
        }
        catch
        {
            return false;
        }
    }
}
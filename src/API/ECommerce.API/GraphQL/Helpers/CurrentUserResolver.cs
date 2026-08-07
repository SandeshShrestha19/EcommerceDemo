using System.Security.Claims;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Models;

namespace ECommerce.API.GraphQL.Helpers;

public static class CurrentUserResolver
{
  public static CurrentUser From(IHttpContextAccessor httpContextAccessor)
  {
    var context = httpContextAccessor.HttpContext
        ?? throw new UnauthorizedException("No HTTP context available.");

    var id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedException("User is not authenticated!");

    var role = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

    return new CurrentUser(Guid.Parse(id), role);
  }
}

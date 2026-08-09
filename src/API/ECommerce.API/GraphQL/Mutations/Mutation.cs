using ECommerce.API.GraphQL.Helpers;
using ECommerce.Domain.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Ports;
using ECommerce.Domain.Exceptions;
using Ecommerce.Domain.Models;
using HotChocolate;
using HotChocolate.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ECommerce.Domain.UseCase;
using ECommerce.Domain.Constants;

namespace ECommerce.API.GraphQL.Mutations;

public class Mutation
{
    //Mutation = Controller
    [Authorize(Roles = ["Admin"])]
    public async Task<Product> AddProduct(
        [Service] IProductFacade productFacade,
        ProductInput productInput,
        CancellationToken cancellationToken) =>
        await productFacade.AddAsync(new AddProductModel
        {
            Name = productInput.Name,
            Description = productInput.Description,
            Price = productInput.Price,
            Stock = productInput.Stock,
            CategoryId = productInput.CategoryId,
            ProductImages = productInput.ProductImages.Select(pi => new ProductImageModel
            {
                ImageUrl = pi.ImageUrl
            }).ToList()
        }, cancellationToken);

    [Authorize(Roles = ["Admin"])]
    public async Task<bool> UpdateProduct([Service] IProductFacade productFacade, Guid id, UpdateProductInput updateProductInput, CancellationToken cancellationToken)
    {
        await productFacade.UpdateAsync(id, new UpdateProductModel
        {
            Name = updateProductInput.Name,
            Description = updateProductInput.Description,
            Price = updateProductInput.Price,
            Stock = updateProductInput.Stock,
            CategoryId = updateProductInput.CategoryId,
            ProductImages = updateProductInput.ProductImages?.Select(pi => new ProductImageModel
            {
                ImageUrl = pi.ImageUrl
            }).ToList()
        }, cancellationToken);
        return true;
    }

    [Authorize(Roles = ["Admin"])]
    public async Task<bool> DeleteProduct(
    [Service] IProductFacade productFacade,
    Guid id,
    CancellationToken cancellationToken) =>
    await productFacade.DeleteAsync(id, cancellationToken);

    [Authorize(Roles = ["Admin"])]
    public async Task<bool> IncreaseProductStock([Service] IProductFacade productFacade, Guid id, int increasingQuantity, CancellationToken cancellationToken)
    {
        await productFacade.IncreaseStockAsync(id, increasingQuantity, cancellationToken);
        return true;
    }

    [Authorize(Roles = ["Admin"])]
    public async Task<bool> DecreaseProductStock([Service] IProductFacade productFacade, Guid id, int decreasingQuantity, CancellationToken cancellationToken)
    {
        await productFacade.DecreaseStockAsync(id, decreasingQuantity, cancellationToken);
        return true;
    }

    public async Task<User> RegisterUser([Service] IUserFacade userFacade, UserInput userInput, CancellationToken cancellationToken)
    {
        return await userFacade.AddAsync(new AddUserModel
        {
            Name = userInput.Name,
            Email = userInput.Email,
            Password = userInput.Password,
            Username = userInput.Username,
            PhoneNumber = userInput.PhoneNumber
        }, cancellationToken);
    }


    [Authorize(Roles = ["Admin"])]
    public async Task<bool> DeleteUser([Service] IUserFacade userFacade, Guid id, CancellationToken cancellationToken)
    {
        return await userFacade.DeleteAsync(id, cancellationToken);
    }

    [Authorize]
    public async Task<bool> UpdateUser([Service] IUserFacade userFacade, [Service] IHttpContextAccessor httpContextAccessor, Guid id, UpdateUserInput updateUserInput, CancellationToken cancellationToken)
    {
        var currentUser = CurrentUserResolver.From(httpContextAccessor);
        if (!currentUser.IsAdmin && currentUser.Id != id)
        {
            throw new ForbiddenException("You can only update your own account!");
        }

        await userFacade.UpdateAsync(id, new UpdateUserModel
        {
            Name = updateUserInput.Name,
            Email = updateUserInput.Email,
            Password = updateUserInput.Password,
            PhoneNumber = updateUserInput.PhoneNumber,
            Username = updateUserInput.Username
        }, cancellationToken);
        return true;
    }

    [Authorize(Policy = "ActiveUser")]
    public async Task<Order> PlaceOrder(
    [Service] IOrderFacade orderFacade,
    [Service] IHttpContextAccessor httpContextAccessor,
    OrderInput orderInput,
    CancellationToken cancellationToken)
    {
        var currentUser = CurrentUserResolver.From(httpContextAccessor);

        return await orderFacade.AddAsync(new PlaceOrderModel
        {
            Items = orderInput.Items.Select(item => new OrderItemModel
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        }, currentUser, cancellationToken);
    }

    [Authorize(Policy = "ActiveUser")]
    public async Task<bool> DeleteOrder([Service] IOrderFacade orderFacade, [Service] IHttpContextAccessor httpContextAccessor, Guid id, CancellationToken cancellationToken)
    {
        return await orderFacade.DeleteAsync(id, CurrentUserResolver.From(httpContextAccessor), cancellationToken);
    }

    [Authorize(Policy = "ActiveUser")]

    public async Task<bool> UpdateOrder(
    [Service] IOrderFacade orderFacade,
    [Service] IHttpContextAccessor httpContextAccessor,
    Guid id,
    UpdateOrderInput updateOrderInput,
    CancellationToken cancellationToken)
    {
        await orderFacade.UpdateAsync(id, new UpdateOrderModel
        {
            OrderStatus = updateOrderInput.OrderStatus,
            Items = updateOrderInput.Items.Select(item => new UpdateOrderItemModel
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        }, CurrentUserResolver.From(httpContextAccessor), cancellationToken);
        return true;
    }

    // Change only the order status (lifecycle). Admins/Managers only; the
    // transition is validated against the order-status lifecycle.
    [Authorize(Roles = ["Admin", "Manager"])]
    public async Task<bool> UpdateOrderStatus(
    [Service] IOrderFacade orderFacade,
    [Service] IHttpContextAccessor httpContextAccessor,
    Guid id,
    OrderStatus orderStatus,
    CancellationToken cancellationToken)
    {
        await orderFacade.UpdateOrderStatusAsync(id, orderStatus, CurrentUserResolver.From(httpContextAccessor), cancellationToken);
        return true;
    }

    [AllowAnonymous]
    public async Task<LoginResponseModel> Login([Service] ILoginUseCase loginUseCase, [Service] IHttpContextAccessor httpContextAccessor, string emailOrUsername,
    string password, CancellationToken cancellationToken)
    {
        var result = await loginUseCase.ExecuteAsync(
            new LoginModel
            {
                EmailOrUsername = emailOrUsername,
                Password = password
            },
            cancellationToken);

        var response = httpContextAccessor
            .HttpContext!
            .Response;

        if (result.RequiresTwoFactor)
        {
            if (string.IsNullOrWhiteSpace(result.TempToken))
            {
                throw new InvalidOperationException(
                    "Temporary 2FA token was not generated."
                );
            }

            response.Cookies.Append(
                "temp2faToken",
                result.TempToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // true in production
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5),
                    Path = "/"
                });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(result.AccessToken))
            {
                throw new InvalidOperationException(
                    "Access token was not generated."
                );
            }

            if (string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Refresh token was not generated."
                );
            }

            response.Cookies.Append(
                "token",
                result.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // required with SameSite=None
                    SameSite = SameSiteMode.None, // allow cross-site (frontend + API on different hosts)
                    Expires = DateTimeOffset.UtcNow.AddMinutes(7),
                    Path = "/"
                });

            response.Cookies.Append(
                "refreshToken",
                result.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // required with SameSite=None
                    SameSite = SameSiteMode.None, // allow cross-site (frontend + API on different hosts)
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    Path = "/"
                });
        }
        return result;
    }

    //clears the cookie
    [Authorize]
    public async Task<bool> Logout([Service] ILogoutUseCase logoutUseCase,
    [Service] IHttpContextAccessor httpContextAccessor,
    CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext!.User
        .FindFirst(ClaimTypes.NameIdentifier)?.Value; //get userId from token

        var jti = httpContextAccessor.HttpContext!.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value; //get jti from token

        var expiry = httpContextAccessor.HttpContext!.User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
        ?? throw new Exception("Token expiry not found!");

        var tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry)).UtcDateTime;

        var refreshToken = httpContextAccessor.HttpContext!.Request.Cookies["refreshToken"] ?? throw new Exception("Refresh token not found!"); //request for refresh token from cookie

        await logoutUseCase.ExecuteAsync(Guid.Parse(userId!), refreshToken, jti!, tokenExpiry, cancellationToken);

        // The delete must match the cookie's attributes (SameSite/Secure) or
        // the browser won't clear the cross-site cookies it has stored.
        httpContextAccessor.HttpContext.Response.Cookies.Delete("token", new CookieOptions
        {
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.None
        });
        httpContextAccessor.HttpContext.Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return true;
    }

    [AllowAnonymous]
    public async Task<string> RefreshToken(
    [Service] IRefreshTokenUseCase refreshTokenUseCase,
    [Service] IHttpContextAccessor httpContextAccessor,
    CancellationToken cancellationToken,
    string? refToken = null)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new Exception("HttpContext not available.");

        var refreshToken = httpContext.Request.Cookies["refreshToken"]
            ?? refToken
            ?? throw new Exception("Refresh token not found!");

        var newAccessToken = await refreshTokenUseCase.ExecuteAsync(refreshToken, cancellationToken);

        httpContext.Response.Cookies.Append(
            "token",
            newAccessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(7)
            });

        return newAccessToken;
    }

    [Authorize(Roles = ["Admin"])]
    public async Task<bool> SetUserActiveStatus([Service] ISetUserActiveStatusUseCase setUserActiveStatusUseCase, Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        await setUserActiveStatusUseCase.ExecuteAsync(userId, isActive, cancellationToken);
        return true;
    }

    // Setup 2FA
    [Authorize]
    public async Task<Enable2FAResponseModel> Setup2FA(
        [Service] IEnable2FAUseCase enable2FAUseCase,
        [Service] IHttpContextAccessor httpContextAccessor,
        string password,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext!.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new Exception("User not found!");

        return await enable2FAUseCase.ExecuteAsync(Guid.Parse(userId), password, cancellationToken);
    }

    // Verify and enable 2FA
    [Authorize]
    public async Task<bool> Verify2FA(
        [Service] IVerify2FAUseCase verify2FAUseCase,
        [Service] IHttpContextAccessor httpContextAccessor,
        string code,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext!.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new Exception("User not found!");

        return await verify2FAUseCase.ExecuteAsync(Guid.Parse(userId), code, cancellationToken);
    }

    [AllowAnonymous]
    public async Task<LoginResponseModel> LoginWith2FA(
        [Service] ILoginWith2FAUseCase loginWith2FAUseCase,
        [Service] IHttpContextAccessor httpContextAccessor,
        string tempToken,
        string code,
        CancellationToken cancellationToken)
    {
        var result = await loginWith2FAUseCase.ExecuteAsync(tempToken, code, cancellationToken);

        httpContextAccessor.HttpContext!.Response.Cookies.Append(
            "token", result.AccessToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(7)
            });

        httpContextAccessor.HttpContext!.Response.Cookies.Append(
            "refreshToken", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

        return result;
    }

    // Disable 2FA
    [Authorize]
    public async Task<bool> Disable2FA(
        [Service] IUserRepository userRepository,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext!.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new Exception("User not found!");

        var user = await userRepository.GetByIdAsync(Guid.Parse(userId), cancellationToken)
            ?? throw new Exception("User not found!");

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    [Authorize]
    public async Task<Category> AddCategory([Service] ICategoryFacade categoryFacade, CategoryInput categoryInput, CancellationToken cancellationToken)
    {
        return await categoryFacade.AddAsync(new AddCategoryModel
        {
            Name = categoryInput.Name
        }, cancellationToken);
    }


    [Authorize]
    public async Task<bool> DeleteCategory([Service] ICategoryFacade categoryFacade, Guid id, CancellationToken cancellationToken)
    {
        return await categoryFacade.DeleteAsync(id, cancellationToken);
    }

    [Authorize]
    public async Task<bool> UpdateCategory([Service] ICategoryFacade categoryFacade, Guid id, UpdateCategoryInput updateCategoryInput, CancellationToken cancellationToken)
    {
        await categoryFacade.UpdateAsync(id, new UpdateCategoryModel
        {
            Name = updateCategoryInput.Name,
            ProductIds = updateCategoryInput.ProductIds
        }, cancellationToken);
        return true;
    }

    [Authorize]
    public async Task<string> GenerateProductDescription(
    [Service] IGeminiFacade geminiFacade,
    string productName,
    string categoryName,
    CancellationToken cancellationToken)
    {
        var prompt = $"""
        Generate a concise e-commerce product description.

        Product: {productName}
        Category: {categoryName}

        Requirements:
        - 2 to 3 sentences
        - Professional tone
        - No exaggerated claims
        """;

        try
        {
            return await geminiFacade.GenerateTextAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            // Surface the real reason (missing key, bad model, quota, blocked
            // content, ...) instead of a generic "Unexpected Execution Error".
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage($"AI description generation failed: {ex.Message}")
                .Build());
        }
    }

    [AllowAnonymous]
    public async Task<LoginResponseModel> GoogleLogin([Service] IGoogleAuthUseCase googleAuthUseCase, [Service] IHttpContextAccessor httpContextAccessor, string idToken)
    {
        var result = await googleAuthUseCase.ExecuteAsync(idToken);

        httpContextAccessor.HttpContext!.Response.Cookies.Append(
            "token", result.AccessToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(7)
            });

        httpContextAccessor.HttpContext!.Response.Cookies.Append(
            "refreshToken", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

        return result;
    }
}
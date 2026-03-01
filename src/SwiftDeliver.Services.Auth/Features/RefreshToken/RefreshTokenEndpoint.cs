using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using SwiftDeliver.Auth.Common.Constants;

namespace SwiftDeliver.Auth.Features.RefreshToken;

public static class RefreshTokenEndpoint
{
    public static IEndpointRouteBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost($"{RouteConstants.Auth}/refresh-token", Handle).AllowAnonymous();
        
        return endpoints;
    }

    private static async Task<Results<Ok, ProblemHttpResult>> Handle(
        HttpContext httpContext,
        IMediator mediator)
    {
        var refreshToken = httpContext.Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return TypedResults.Problem(
                title: "Bad Request",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Refresh token is missing",
                instance: $"{httpContext.Request.Method} {httpContext.Request.Path}");
        }
        
        var result = await mediator.Send(new RefreshTokenEndpointCommand(refreshToken));
        
        if (!result.IsSuccess)
        {
            return TypedResults.Problem(
                title: "Bad Request",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Failed to refresh token",
                instance: $"{httpContext.Request.Method} {httpContext.Request.Path}",
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = result.Errors.Select(e => e.Message)
                });
        }

        var accessTokenCookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMilliseconds(AuthConstants.AccessTokenExpirationMs),
        };
        
        var refreshTokenCookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs),
        };
        
        httpContext.Response.Cookies.Append("accessToken", result.Value.AccessToken, accessTokenCookieOptions);
        httpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken, refreshTokenCookieOptions);
        
        return TypedResults.Ok();
    }
}
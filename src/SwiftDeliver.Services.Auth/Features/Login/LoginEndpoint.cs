using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using SwiftDeliver.Auth.Common.Constants;

namespace SwiftDeliver.Auth.Features.Login;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost($"{RouteConstants.Auth}/login", Handle).AllowAnonymous();

        return endpoints;
    }

    private static async Task<Results<Ok, ProblemHttpResult>> Handle(IMediator mediator, LoginEndpointCommand command,
        HttpContext httpContext)
    {
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            return TypedResults.Problem(
                title: "Bad Request", 
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = result.Errors
                });
        }

        var accessTokenCookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Expires = DateTime.Now.AddMilliseconds(AuthConstants.AccessTokenExpirationMs),
        };
        
        var refreshTokenCookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Expires = DateTime.Now.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs),
        };
        
        httpContext.Response.Cookies.Append("accessToken", result.Value.AccessToken, accessTokenCookieOptions);
        httpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken, refreshTokenCookieOptions);

        return TypedResults.Ok();
    }
}
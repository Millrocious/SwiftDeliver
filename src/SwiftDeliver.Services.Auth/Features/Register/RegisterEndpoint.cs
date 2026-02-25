using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using SwiftDeliver.Auth.Common.Constants;

namespace SwiftDeliver.Auth.Features.Register;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegisterEndpoint(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapPost($"{RouteConstants.Auth}/register", Handle).AllowAnonymous();
        
        return endpoint;
    }

    private static async Task<Results<Ok<RegisterEndpointResponse>, ProblemHttpResult>> Handle(
        HttpContext httpContext,
        RegisterEndpointCommand command, 
        IMediator mediator)
    {
        var res = await mediator.Send(command);

        if (!res.IsSuccess)
        {
            return TypedResults.Problem(
                detail: res.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", res.Errors.Select(e => e.Message) }
                });
        }
        
        var accessTokenCookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddMilliseconds(AuthConstants.AccessTokenExpirationMs),
            HttpOnly = true,
        };
        
        var refreshTokenCookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs),
            HttpOnly = true,
        };
        
        httpContext.Response.Cookies.Append("accessToken", res.Value.AccessToken, accessTokenCookieOptions);
        httpContext.Response.Cookies.Append("refreshToken", res.Value.RefreshToken, refreshTokenCookieOptions);
        
        return TypedResults.Ok(new RegisterEndpointResponse(res.Value.UserId));
    }
}
namespace SwiftDeliver.Auth.Features.Register;

public sealed record RegisterEndpointTokens(Guid UserId, string AccessToken, string RefreshToken);
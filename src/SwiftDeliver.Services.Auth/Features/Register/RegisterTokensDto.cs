namespace SwiftDeliver.Auth.Features.Register;

public sealed record RegisterTokensDto(Guid UserId, string AccessToken, string RefreshToken);
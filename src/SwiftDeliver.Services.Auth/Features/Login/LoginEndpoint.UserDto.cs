namespace SwiftDeliver.Auth.Features.Login;

public sealed record LoginEndpointUserDto(Guid Id, string Email, string PasswordHash, string PasswordSalt);
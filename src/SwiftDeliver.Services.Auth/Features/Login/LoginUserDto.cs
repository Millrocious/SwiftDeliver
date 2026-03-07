namespace SwiftDeliver.Auth.Features.Login;

public sealed record LoginUserDto(Guid Id, string Email, string PasswordHash, string PasswordSalt);
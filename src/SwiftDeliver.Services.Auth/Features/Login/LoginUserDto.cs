namespace SwiftDeliver.Auth.Features.Login;

public sealed record LoginUserDto(Guid Id, string Email, Guid RoleId, string PasswordHash, string PasswordSalt);
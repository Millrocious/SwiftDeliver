namespace SwiftDeliver.Auth.Features.Login;

public static class LoginQueries
{
    public const string UserDtoSql = "SELECT Id, Email, PasswordHash, PasswordSalt FROM Users WHERE Email = @Email";
    public const string InsertNewRefreshTokenSql = """
                                                   INSERT INTO RefreshTokens(Token, UserId, ExpiresAt)
                                                   OUTPUT inserted.Id
                                                   VALUES(@Token, @UserId, @ExpiresAt)
                                                   """;
    public const string RevokeTokenSql = """
                                         UPDATE RefreshTokens 
                                         SET IsRevoked = 1 
                                         WHERE UserId = @UserId AND Id != @Id
                                         """;
}
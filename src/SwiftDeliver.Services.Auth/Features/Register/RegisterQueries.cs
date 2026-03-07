namespace SwiftDeliver.Auth.Features.Register;

public static class RegisterQueries
{
    public const string IsUserExistsSql = """
                                          SELECT IIF(EXISTS 
                                              (SELECT 1 FROM Users WHERE Email = @Email), 1, 0)
                                          """;

    public const string CreateUserSql = """
                                        INSERT INTO Users(Email, PasswordHash, PasswordSalt, RoleId, CreatedAt) 
                                        OUTPUT INSERTED.Id
                                        SELECT @Email, @PasswordHash, @PasswordSalt, Id, @CreatedAt
                                        FROM Roles r 
                                        WHERE r.Name = 'Client'
                                        """;

    public const string InsertRefreshTokenSql = """
                                                INSERT INTO RefreshTokens(UserId, Token, ExpiresAt)
                                                VALUES(@UserId, @Token, @ExpiresAt)
                                                """;
}
namespace SwiftDeliver.Auth.Features.RefreshToken;

public static class RefreshTokenQueries
{
    public const string GetRefreshTokenSql = """
                                             SELECT u.Id, u.Email, u.RoleId
                                             FROM Users u 
                                                 INNER JOIN RefreshTokens rt ON u.Id = rt.UserId 
                                             WHERE rt.Token = @Token
                                             """;

    public const string RevokeOldRefreshTokenSql = """
                                                   UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @Token
                                                   """;

    public const string InsertNewRefreshTokenSql = """
                                                   INSERT INTO RefreshTokens(Token, UserId, ExpiresAt) 
                                                   VALUES(@Token, @UserId, @ExpiresAt)
                                                   """;

    public const string UserRoleSql = """
                                      SELECT r.Name FROM Roles r
                                      WHERE r.Id = @RoleId
                                      """;
}
using System.Data;
using Dapper;
using FluentResults;
using Mediator;
using SwiftDeliver.Auth.Common.Constants;
using SwiftDeliver.Auth.Common.Interfaces;

namespace SwiftDeliver.Auth.Features.RefreshToken;

public sealed class RefreshTokenEndpointHandler : ICommandHandler<RefreshTokenEndpointCommand, Result<RefreshTokenEndpointTokens>>
{
    private readonly IDbConnection _connection;
    private readonly ITokenGenerator _tokenGenerator;
    
    public RefreshTokenEndpointHandler(IDbConnection connection, ITokenGenerator tokenGenerator)
    {
        _connection = connection;
        _tokenGenerator = tokenGenerator;
    }

    public async ValueTask<Result<RefreshTokenEndpointTokens>> Handle(RefreshTokenEndpointCommand command, CancellationToken cancellationToken)
    {
        _connection.Open();
        var getRefreshTokenSql = """
                                 SELECT u.Id, u.Email 
                                 FROM Users u 
                                     INNER JOIN RefreshTokens rt ON u.Id = rt.UserId 
                                 WHERE rt.Token = @Token
                                 """;

        var result = await _connection.QuerySingleOrDefaultAsync<RefreshTokenEndpointUserDto>(
            getRefreshTokenSql,
            new { Token = command.RefreshToken });

        if (result == null)
        {
            return Result.Fail("");
        }
        
        using var transaction = _connection.BeginTransaction();

        try
        {
            var revokeOldRefreshTokenSql = """
                                           UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @Token
                                           """;
        
            await _connection.ExecuteAsync(
                revokeOldRefreshTokenSql, 
                new { Token = command.RefreshToken },
                transaction);
        
            var newRefreshToken = Guid.NewGuid().ToString();

            var insertNewRefreshTokenSql = """
                                           INSERT INTO RefreshTokens(Token, UserId, ExpiresAt) 
                                           VALUES(@Token, @UserId, @ExpiresAt)
                                           """;

            await _connection.ExecuteAsync(
                insertNewRefreshTokenSql,
                new
                {
                    Token = newRefreshToken,
                    UserId = result.Id,
                    ExpiresAt = DateTime.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs)
                },
                transaction);
        
            var newAccessToken = _tokenGenerator.GenerateToken(result.Email);
        
            transaction.Commit();
            
            return Result.Ok(new RefreshTokenEndpointTokens(newAccessToken, newRefreshToken));
        }
        catch (Exception e)
        {
            transaction.Rollback();
            
            throw;
        }
    }
}
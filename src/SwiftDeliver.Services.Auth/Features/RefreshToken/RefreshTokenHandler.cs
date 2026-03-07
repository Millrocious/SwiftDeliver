using System.Data;
using Dapper;
using FluentResults;
using Mediator;
using SwiftDeliver.Auth.Common.Constants;
using SwiftDeliver.Auth.Common.Interfaces;

namespace SwiftDeliver.Auth.Features.RefreshToken;

public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, Result<RefreshTokenTokens>>
{
    private readonly IDbConnection _connection;
    private readonly ITokenGenerator _tokenGenerator;
    
    public RefreshTokenHandler(IDbConnection connection, ITokenGenerator tokenGenerator)
    {
        _connection = connection;
        _tokenGenerator = tokenGenerator;
    }

    public async ValueTask<Result<RefreshTokenTokens>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        _connection.Open();

        var result = await _connection.QuerySingleOrDefaultAsync<RefreshTokenUserDto>(
            RefreshTokenQueries.GetRefreshTokenSql,
            new { Token = command.RefreshToken });

        if (result == null)
        {
            return Result.Fail("");
        }
        
        using var transaction = _connection.BeginTransaction();

        try
        {
            await _connection.ExecuteAsync(
                RefreshTokenQueries.RevokeOldRefreshTokenSql, 
                new { Token = command.RefreshToken },
                transaction);
        
            var newRefreshToken = Guid.NewGuid().ToString();
            
            await _connection.ExecuteAsync(
                RefreshTokenQueries.InsertNewRefreshTokenSql,
                new
                {
                    Token = newRefreshToken,
                    UserId = result.Id,
                    ExpiresAt = DateTime.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs)
                },
                transaction);
        
            var newAccessToken = _tokenGenerator.GenerateToken(result.Email);
        
            transaction.Commit();
            
            return Result.Ok(new RefreshTokenTokens(newAccessToken, newRefreshToken));
        }
        catch (Exception e)
        {
            transaction.Rollback();
            
            throw;
        }
    }
}
using System.Data;
using System.Security.Cryptography;
using Dapper;
using FluentResults;
using Mediator;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using SwiftDeliver.Auth.Common.Constants;
using SwiftDeliver.Auth.Common.Interfaces;

namespace SwiftDeliver.Auth.Features.Login;

public class LoginHandler : ICommandHandler<LoginCommand, Result<LoginTokensDto>>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<LoginHandler> _logger;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginHandler(
        IDbConnection connection, 
        ILogger<LoginHandler> logger, 
        ITokenGenerator tokenGenerator)
    {
        _connection = connection;
        _logger = logger;
        _tokenGenerator = tokenGenerator;
    }

    public async ValueTask<Result<LoginTokensDto>> Handle(
        LoginCommand command, 
        CancellationToken cancellationToken)
    {
        _connection.Open();
        
        var userDto = await _connection.QueryFirstOrDefaultAsync<LoginUserDto>(
            LoginQueries.UserDtoSql, 
            new { Email = command.Email });
        
        if (userDto is null)
        {
            return Result.Fail("User not found");
        }
        
        using var transaction = _connection.BeginTransaction();
        
        try
        {
            var userPasswordHashBytes = Convert.FromBase64String(userDto.PasswordHash);
            var userPasswordSaltBytes = Convert.FromBase64String(userDto.PasswordSalt);
        
            var passwordHashBytes = KeyDerivation.Pbkdf2(
                password: command.Password,
                salt: userPasswordSaltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8);

            if (!CryptographicOperations.FixedTimeEquals(userPasswordHashBytes, passwordHashBytes))
            {
                return Result.Fail("Invalid credentials");
            }
            
            var newRefreshToken = Guid.NewGuid().ToString();
        
            var newRefreshTokenId = await _connection.ExecuteScalarAsync<Guid>(
                LoginQueries.InsertNewRefreshTokenSql, 
                new
                {
                    Token = newRefreshToken, 
                    UserId = userDto.Id, 
                    ExpiresAt = DateTime.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs)
                },
                transaction);
            
            await _connection.ExecuteAsync(
                LoginQueries.RevokeTokenSql,
                new { UserId = userDto.Id, Id = newRefreshTokenId },
                transaction);
            
            var userRoleName = await _connection.QuerySingleAsync<string>(
                LoginQueries.UserRoleSql,
                new
                {
                    RoleId = userDto.RoleId
                },
                transaction);

            var newAccessToken = _tokenGenerator.GenerateToken(userDto.Email, userRoleName);
            
            transaction.Commit();
            
            return Result.Ok(new LoginTokensDto(newAccessToken, newRefreshToken));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during login endpoint.");
            transaction.Rollback();

            throw;
        }
    }
}
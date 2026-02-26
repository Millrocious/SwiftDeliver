using System.Data;
using System.Security.Cryptography;
using Dapper;
using FluentResults;
using Mediator;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using SwiftDeliver.Auth.Common.Constants;
using SwiftDeliver.Auth.Common.Interfaces;

namespace SwiftDeliver.Auth.Features.Login;

public class LoginEndpointHandler : ICommandHandler<LoginEndpointCommand, Result<LoginEndpointTokens>>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<LoginEndpointHandler> _logger;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginEndpointHandler(
        IDbConnection connection, 
        ILogger<LoginEndpointHandler> logger, 
        ITokenGenerator tokenGenerator)
    {
        _connection = connection;
        _logger = logger;
        _tokenGenerator = tokenGenerator;
    }

    public async ValueTask<Result<LoginEndpointTokens>> Handle(
        LoginEndpointCommand command, 
        CancellationToken cancellationToken)
    {
        _connection.Open();
        
        var userDtoSql = "SELECT Id, Email, PasswordHash, PasswordSalt FROM Users WHERE Email = @Email";
        
        var userDto = await _connection.QueryFirstOrDefaultAsync<LoginEndpointUserDto>(
            userDtoSql, 
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
            var insertNewRefreshTokenSql = """
                                           INSERT INTO RefreshTokens(Token, UserId, ExpiresAt)
                                           OUTPUT inserted.Id
                                           VALUES(@Token, @UserId, @ExpiresAt)
                                           """;
        
            var newRefreshTokenId = await _connection.ExecuteScalarAsync<Guid>(
                insertNewRefreshTokenSql, 
                new
                {
                    Token = newRefreshToken, 
                    UserId = userDto.Id, 
                    ExpiresAt = DateTime.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs)
                },
                transaction);
            
            var revokeTokenSql = """
                                 UPDATE RefreshTokens 
                                 SET IsRevoked = 1 
                                 WHERE UserId = @UserId AND Id != @Id
                                 """;
            
            await _connection.ExecuteAsync(
                revokeTokenSql,
                new { UserId = userDto.Id, Id = newRefreshTokenId },
                transaction);

            var newAccessToken = _tokenGenerator.GenerateToken(userDto.Email);
            
            transaction.Commit();
            
            return Result.Ok(new LoginEndpointTokens(newAccessToken, newRefreshToken));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during login endpoint.");
            transaction.Rollback();

            throw;
        }
    }
}
using System.Data;
using System.Security.Cryptography;
using Dapper;
using FluentResults;
using Mediator;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using SwiftDeliver.Auth.Common.Constants;
using SwiftDeliver.Auth.Common.Interfaces;

namespace SwiftDeliver.Auth.Features.Register;

public class RegisterEndpointHandler : ICommandHandler<RegisterEndpointCommand, Result<RegisterEndpointTokens>>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<RegisterEndpointHandler> _logger;
    private readonly ITokenGenerator _tokenGenerator;

    public RegisterEndpointHandler(
        IDbConnection connection, 
        ILogger<RegisterEndpointHandler> logger, 
        ITokenGenerator tokenGenerator)
    {
        _connection = connection;
        _logger = logger;
        _tokenGenerator = tokenGenerator;
    }

    public async ValueTask<Result<RegisterEndpointTokens>> Handle(RegisterEndpointCommand command,
        CancellationToken cancellationToken)
    {
        _connection.Open();
        using var transaction = _connection.BeginTransaction();

        const string isUserExistsSql = """
                                       SELECT IIF(EXISTS 
                                           (SELECT 1 FROM Users WHERE Email = @Email), 1, 0)
                                       """;
        
        var isUserExists = await _connection.ExecuteScalarAsync<bool>(
            isUserExistsSql, 
            new { Email = command.Email },
            transaction);

        if (isUserExists)
        {
            _logger.LogInformation("User with email: {email} already exists", command.Email);
            return Result.Fail("User with this email already exists");
        }
        
        var saltBytes = RandomNumberGenerator.GetBytes(128 / 8);
        var saltBase64 = Convert.ToBase64String(saltBytes);

        var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: command.Password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
        
        try
        {
            var createUserSql = """
                      INSERT INTO Users(Email, PasswordHash, PasswordSalt, RoleId, CreatedAt) 
                      OUTPUT INSERTED.Id
                      SELECT @Email, @PasswordHash, @PasswordSalt, Id, @CreatedAt
                      FROM Roles r 
                      WHERE r.Name = 'Client'
                      """;

            var createdUserId = await _connection.QuerySingleAsync<Guid>(createUserSql,
                new
                {
                    Email = command.Email, 
                    PasswordHash = hashed, 
                    PasswordSalt = saltBase64, 
                    CreatedAt = DateTime.UtcNow
                }, 
                transaction);

            var accessToken = _tokenGenerator.GenerateToken(command.Email);
            var refreshToken = Guid.NewGuid().ToString();

            const string insertRefreshTokenSql = """
                                                 INSERT INTO RefreshTokens(UserId, Token, ExpiresAt)
                                                 VALUES(@UserId, @Token, @ExpiresAt)
                                                 """;
        
            await _connection.ExecuteAsync(
                insertRefreshTokenSql, 
                new
                {
                    UserId = createdUserId, 
                    Token = refreshToken, 
                    ExpiresAt = DateTime.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs)
                }, 
                transaction);
            
            transaction.Commit();
            
            return Result.Ok(new RegisterEndpointTokens(createdUserId, accessToken, refreshToken));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occured while registering user.");
            transaction.Rollback();

            throw;
        }
    }
}
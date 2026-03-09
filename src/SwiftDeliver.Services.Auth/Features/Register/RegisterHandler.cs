using System.Data;
using System.Security.Cryptography;
using Dapper;
using FluentResults;
using Mediator;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using SwiftDeliver.Auth.Common.Constants;
using SwiftDeliver.Auth.Common.Interfaces;

namespace SwiftDeliver.Auth.Features.Register;

public class RegisterHandler : ICommandHandler<RegisterCommand, Result<RegisterTokensDto>>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly ITokenGenerator _tokenGenerator;

    public RegisterHandler(
        IDbConnection connection,
        ILogger<RegisterHandler> logger,
        ITokenGenerator tokenGenerator)
    {
        _connection = connection;
        _logger = logger;
        _tokenGenerator = tokenGenerator;
    }

    public async ValueTask<Result<RegisterTokensDto>> Handle(RegisterCommand command,
        CancellationToken cancellationToken)
    {
        _connection.Open();
        using var transaction = _connection.BeginTransaction();

        var isUserExists = await _connection.ExecuteScalarAsync<bool>(
            RegisterQueries.IsUserExistsSql,
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
            var createdUser = await _connection.QuerySingleAsync<RegisterUserDto>(
                RegisterQueries.CreateUserSql,
                new
                {
                    Email = command.Email,
                    PasswordHash = hashed,
                    PasswordSalt = saltBase64,
                    CreatedAt = DateTime.UtcNow
                },
                transaction);

            var userRoleName = await _connection.QuerySingleAsync<string>(
                RegisterQueries.UserRoleSql,
                new
                {
                    RoleId = createdUser.RoleId,
                },
                transaction);

            var accessToken = _tokenGenerator.GenerateToken(command.Email, userRoleName);
            var refreshToken = Guid.NewGuid().ToString();

            await _connection.ExecuteAsync(
                RegisterQueries.InsertRefreshTokenSql,
                new
                {
                    UserId = createdUser.Id,
                    Token = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMilliseconds(AuthConstants.RefreshTokenExpirationMs)
                },
                transaction);

            transaction.Commit();

            return Result.Ok(new RegisterTokensDto(createdUser.Id, accessToken, refreshToken));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occured while registering user.");
            transaction.Rollback();

            throw;
        }
    }
}
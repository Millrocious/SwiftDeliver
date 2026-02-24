using System.Data;
using System.Security.Cryptography;
using Dapper;
using FluentResults;
using Mediator;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace SwiftDeliver.Auth.Features.Register;

public class RegisterEndpointHandler : ICommandHandler<RegisterEndpointCommand, Result<RegisterEndpointResponse>>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<RegisterEndpointHandler> _logger;

    public RegisterEndpointHandler(IDbConnection connection, ILogger<RegisterEndpointHandler> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async ValueTask<Result<RegisterEndpointResponse>> Handle(RegisterEndpointCommand command,
        CancellationToken cancellationToken)
    {
        var isUserExistsSql = "SELECT IIF(EXISTS (SELECT 1 FROM Users WHERE Email = @Email), 1, 0)";
        
        var isUserExists = await _connection.ExecuteScalarAsync<bool>(
            isUserExistsSql, 
            new { Email = command.Email });

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

        var sql = """
                  INSERT INTO Users(Email, PasswordHash, PasswordSalt, RoleId, CreatedAt) 
                  OUTPUT INSERTED.Id
                  SELECT @Email, @PasswordHash, @PasswordSalt, Id, @CreatedAt
                  FROM Roles r 
                  WHERE r.Name = 'Client'
                  """;

        var result = await _connection.QuerySingleAsync<Guid>(sql,
            new { Email = command.Email, PasswordHash = hashed, PasswordSalt = saltBase64, CreatedAt = DateTime.UtcNow });

        return Result.Ok(new RegisterEndpointResponse(result));
    }
}
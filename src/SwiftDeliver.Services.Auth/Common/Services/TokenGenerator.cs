using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SwiftDeliver.Auth.Common.Settings;

namespace SwiftDeliver.Auth.Common.Services;

public class TokenGenerator
{
    private readonly JwtSettings _options;

    public TokenGenerator(IOptions<JwtSettings> options)
    {
        _options = options.Value;
    }
    
    public string GenerateToken(string email)
    {
       var secretKey = Encoding.UTF8.GetBytes(_options.SecretKey);
       var symmetricKey = new SymmetricSecurityKey(secretKey);

       var claims = new Dictionary<string, object>()
       {
           [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
           [JwtRegisteredClaimNames.Sub] = email,
           [JwtRegisteredClaimNames.Email] = email,
       };

       var securityTokenDescriptor = new SecurityTokenDescriptor()
       {
           Issuer = _options.Issuer,
           Audience = _options.Audience,
           Claims = claims,
           Expires = DateTime.UtcNow.AddMinutes(15),
           IssuedAt = DateTime.UtcNow,
           NotBefore = DateTime.UtcNow,
           SigningCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256Signature)
       };

       var tokenHandler = new JsonWebTokenHandler();
       var tokenString = tokenHandler.CreateToken(securityTokenDescriptor);

       return tokenString;
    }
}
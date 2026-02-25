namespace SwiftDeliver.Auth.Common.Interfaces;

public interface ITokenGenerator
{
    public string GenerateToken(string email);
}
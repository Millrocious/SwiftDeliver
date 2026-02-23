using SwiftDeliver.Auth.Common.Settings;

namespace SwiftDeliver.Auth.Common.Extensions;

public static class OptionsExtensions
{
    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        
        return services;
    }
}
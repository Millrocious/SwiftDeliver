using System.Data;
using Microsoft.Data.SqlClient;
using SwiftDeliver.Auth.Common.Settings;

namespace SwiftDeliver.Auth.Common.Extensions;

public static class DbExtensions
{
    public static IServiceCollection ConfigureDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("AuthDb");

        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException("No connection string found in the configuration file");
        }
        
        services.AddScoped<IDbConnection>(_ => new SqlConnection(connString));
        
        return services;
    }
}
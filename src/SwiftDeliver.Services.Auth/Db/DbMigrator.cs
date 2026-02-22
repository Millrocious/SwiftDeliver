using EvolveDb;
using Microsoft.Data.SqlClient;
using Serilog;

namespace SwiftDeliver.Auth.Db;

public static class DbMigrator
{
    public static void Migrate(string? connectionString)
    {
        try
        {
            var cnx = new SqlConnection(connectionString);
            var evolve = new Evolve(cnx, msg => Log.Logger.Information(msg))
            {
                Locations = ["Db/Migrations"],
                IsEraseDisabled = true,
            };

            evolve.Migrate();
        }
        catch (Exception ex)
        {
            Log.Logger.Fatal("Database migration failed.", ex);
            throw;
        }
    }
}
using Serilog;
using SwiftDeliver.Auth.Db;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => 
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

DbMigrator.Migrate(builder.Configuration.GetConnectionString("AuthDb"));

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Hello World!");

app.Run();
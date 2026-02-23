using Serilog;
using SwiftDeliver.Auth.Common.Extensions;
using SwiftDeliver.Auth.Db;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => 
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);

var app = builder.Build();

DbMigrator.Migrate(app.Configuration.GetConnectionString("AuthDb"));

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Hello World!").RequireAuthorization();

app.Run();
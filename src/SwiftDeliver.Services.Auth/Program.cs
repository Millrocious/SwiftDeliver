using Serilog;
using SwiftDeliver.Auth.Common.Extensions;
using SwiftDeliver.Auth.Db;
using SwiftDeliver.Auth.Features.Login;
using SwiftDeliver.Auth.Features.RefreshToken;
using SwiftDeliver.Auth.Features.Register;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => 
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddProblemDetails();

builder.Services.AddOptions(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.ConfigureDb(builder.Configuration);

builder.Services.AddMediator(options => {
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

var app = builder.Build();

DbMigrator.Migrate(app.Configuration.GetConnectionString("AuthDb"));

app.UseStatusCodePages();
app.UseSerilogRequestLogging();

app.MapGet("/error", () => {
    throw new Exception("Щось пішло зовсім не так!");
});

app.MapGet("/", () => "Hello World!").RequireAuthorization();

app.MapRegisterEndpoint();
app.MapLoginEndpoint();
app.MapRefreshTokenEndpoint();

app.Run();
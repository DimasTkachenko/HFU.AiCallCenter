using Hfu.VoiceRegistration.Application;
using Hfu.VoiceRegistration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () =>
{
    var version = typeof(Program).Assembly.GetName().Version?.ToString();

    return Results.Ok(new HealthResponse(
        Status: "healthy",
        Service: "Hfu.VoiceRegistration.Api",
        TimestampUtc: DateTimeOffset.UtcNow,
        Version: version));
});

app.Run();

internal sealed record HealthResponse(
    string Status,
    string Service,
    DateTimeOffset TimestampUtc,
    string? Version);

public partial class Program;

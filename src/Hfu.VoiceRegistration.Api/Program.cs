using System.Text.Json.Serialization;
using Hfu.VoiceRegistration.Api.Endpoints;
using Hfu.VoiceRegistration.Application;
using Hfu.VoiceRegistration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () =>
{
    var version = typeof(Program).Assembly.GetName().Version?.ToString();

    return Results.Ok(new HealthResponse(
        Status: "healthy",
        Service: "Hfu.VoiceRegistration.Api",
        TimestampUtc: DateTimeOffset.UtcNow,
        Version: version));
});

app.MapConversationSessionEndpoints();
app.MapRegistrationToolEndpoints();
app.MapReferenceDataEndpoints();

app.Run();

internal sealed record HealthResponse(
    string Status,
    string Service,
    DateTimeOffset TimestampUtc,
    string? Version);

public partial class Program;

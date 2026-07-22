using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hfu.VoiceRegistration.Api.Endpoints;
using Hfu.VoiceRegistration.Api.OpenAIRealtime;
using Hfu.VoiceRegistration.Api.Realtime;
using Hfu.VoiceRegistration.Application;
using Hfu.VoiceRegistration.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<OpenAIRealtimeOptions>(
    builder.Configuration.GetSection(OpenAIRealtimeOptions.SectionName));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHttpClient<IOpenAIRealtimeClient, OpenAIRealtimeClient>();
builder.Services.AddSingleton<IConversationRealtimeNotifier, SignalRConversationRealtimeNotifier>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(OpenAIRealtimeEndpoints.RateLimitPolicyName, httpContext =>
    {
        var openAIOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<OpenAIRealtimeOptions>>()
            .Value;
        var remoteAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var sessionId = httpContext.Request.RouteValues.TryGetValue("sessionId", out var value)
            ? value?.ToString() ?? "unknown"
            : "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{remoteAddress}:{sessionId}",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = openAIOptions.EffectiveRealtimeCallsPerMinute,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseRateLimiter();

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
app.MapOpenAIRealtimeEndpoints();
app.MapHub<ConversationHub>("/hubs/conversation");

app.Run();

internal sealed record HealthResponse(
    string Status,
    string Service,
    DateTimeOffset TimestampUtc,
    string? Version);

public partial class Program;

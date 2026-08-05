using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.Persistence;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Infrastructure.Conversations;
using Hfu.VoiceRegistration.Infrastructure.Persistence;
using Hfu.VoiceRegistration.Infrastructure.RegistrationCompletion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hfu.VoiceRegistration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ConversationSessionStoreOptions>()
            .Bind(configuration.GetSection("ConversationSessions"));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IConversationSessionStore, InMemoryConversationSessionStore>();
        services.AddSingleton<IRegistrationIdGenerator, InMemoryDemoRegistrationIdGenerator>();
        services.AddScoped<IFakeHfuRegistrationService, FakeHfuRegistrationService>();
        services.AddHostedService<ConversationSessionCleanupService>();

        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"]
            ?? configuration["ConnectionStrings:DefaultConnection"];

        if (!string.IsNullOrWhiteSpace(rawConnectionString))
        {
            var formattedConnectionString = FormatPostgresConnectionString(rawConnectionString);

            services.AddDbContext<VoiceRegistrationDbContext>(options =>
            {
                options.UseNpgsql(formattedConnectionString);
            });

            services.AddScoped<IRegistrationRepository, PostgresRegistrationRepository>();
        }

        return services;
    }

    public static string FormatPostgresConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var database = uri.AbsolutePath.TrimStart('/');

            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = database,
                Username = username,
                Password = password,
                SslMode = Npgsql.SslMode.Require,
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }

        return connectionString;
    }
}

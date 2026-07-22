using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

internal static class ApiIntegrationTestHelpers
{
    public static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/conversation-sessions", new { });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using var document = await ReadJsonAsync(response);
        var sessionId = document.RootElement.GetProperty("sessionId").GetGuid();

        Assert.Contains(
            $"/api/conversation-sessions/{sessionId}",
            response.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);

        return sessionId;
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    public static async Task<JsonDocument> CompleteDemoRegistrationAsync(
        HttpClient client,
        Guid sessionId)
    {
        using var updateResponse = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/tools/update-registration-fields",
            new
            {
                fields = new object[]
                {
                    new { name = "firstName", value = "Dimas" },
                    new { name = "lastName", value = "Tkachenko" },
                    new { name = "dateOfBirth", value = "1991-08-24" },
                    new { name = "phoneNumber", value = "+380501112233" },
                    new { name = "currentRegion", value = "Харківська область" },
                    new { name = "currentCity", value = "Харків" },
                    new { name = "userCategory", value = "Other" }
                }
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/tools/confirm-registration-fields",
            new
            {
                fieldNames = new[]
                {
                    "firstName",
                    "lastName",
                    "dateOfBirth",
                    "phoneNumber",
                    "currentRegion",
                    "currentCity",
                    "userCategory"
                }
            });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        using var completeResponse = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/tools/complete-registration",
            new
            {
                personalDataConsent = true,
                registrationConfirmed = true
            });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        return await ReadJsonAsync(completeResponse);
    }
}

using Hfu.VoiceRegistration.Api.GeminiLive;
using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class GeminiLiveToolsBuilderTests
{
    [Fact]
    public void BuildRegistrationToolsReturnsValidDeclarations()
    {
        var declaration = GeminiLiveToolsBuilder.BuildRegistrationTools();

        Assert.NotNull(declaration);
        Assert.NotNull(declaration.FunctionDeclarations);
        Assert.Equal(6, declaration.FunctionDeclarations.Count);

        var names = declaration.FunctionDeclarations.Select(f => f.Name).ToList();
        Assert.Contains("update_registration_fields", names);
        Assert.Contains("confirm_registration_fields", names);
        Assert.Contains("mark_fields_for_clarification", names);
        Assert.Contains("clear_registration_fields", names);
        Assert.Contains("get_registration_state", names);
        Assert.Contains("complete_registration", names);
    }
}

namespace Hfu.VoiceRegistration.Application.RegistrationCompletion;

public interface IFakeHfuRegistrationService
{
    Task<FakeHfuRegistrationResponse> RegisterAsync(
        FinalRegistrationDto registration,
        CancellationToken cancellationToken);
}

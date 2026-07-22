using Hfu.VoiceRegistration.Application.RegistrationCompletion;

namespace Hfu.VoiceRegistration.Infrastructure.RegistrationCompletion;

public sealed class FakeHfuRegistrationService : IFakeHfuRegistrationService
{
    private readonly IRegistrationIdGenerator _registrationIdGenerator;
    private readonly TimeProvider _timeProvider;

    public FakeHfuRegistrationService(
        IRegistrationIdGenerator registrationIdGenerator,
        TimeProvider timeProvider)
    {
        _registrationIdGenerator = registrationIdGenerator;
        _timeProvider = timeProvider;
    }

    public Task<FakeHfuRegistrationResponse> RegisterAsync(
        FinalRegistrationDto registration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);
        cancellationToken.ThrowIfCancellationRequested();

        var now = _timeProvider.GetUtcNow();
        var registrationId = _registrationIdGenerator.Generate(now);

        return Task.FromResult(new FakeHfuRegistrationResponse(
            Success: true,
            RegistrationId: registrationId,
            Message: "Registration completed",
            CompletedAt: now));
    }
}

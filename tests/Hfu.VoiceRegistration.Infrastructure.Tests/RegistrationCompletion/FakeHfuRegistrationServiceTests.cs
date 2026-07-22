using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Domain.Registration;
using Hfu.VoiceRegistration.Infrastructure.RegistrationCompletion;

namespace Hfu.VoiceRegistration.Infrastructure.Tests.RegistrationCompletion;

public sealed class FakeHfuRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsyncReturnsSuccessfulDemoRegistration()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var service = new FakeHfuRegistrationService(
            new InMemoryDemoRegistrationIdGenerator(),
            new FakeTimeProvider(now));

        var response = await service.RegisterAsync(CreateFinalDto(), CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal("DEMO-2026-000001", response.RegistrationId);
        Assert.Equal(now, response.CompletedAt);
        Assert.Equal("Registration completed", response.Message);
    }

    private static FinalRegistrationDto CreateFinalDto()
    {
        return new FinalRegistrationDto(
            FirstName: "Dimas",
            LastName: "Tkachenko",
            Patronymic: null,
            DateOfBirth: new DateOnly(1991, 8, 24),
            PhoneNumber: "+380501112233",
            Email: null,
            CurrentRegion: "Kharkivska oblast",
            CurrentRegionReferenceId: "hfu-region-kharkivska",
            CurrentCity: "Kharkiv",
            ActualAddress: null,
            UserCategory: UserCategory.Other,
            RegionBeforeWar: null,
            RegionBeforeWarReferenceId: null,
            DisplacedCertificateYear: null,
            PersonalDataConsent: true,
            RegistrationConfirmed: true);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}

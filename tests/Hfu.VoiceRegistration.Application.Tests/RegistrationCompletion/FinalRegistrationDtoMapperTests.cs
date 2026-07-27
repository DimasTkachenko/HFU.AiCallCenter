using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Application.Tests.RegistrationCompletion;

public sealed class FinalRegistrationDtoMapperTests
{
    [Fact]
    public void MapCreatesFinalDtoFromServerOwnedDraft()
    {
        var draft = RegistrationDraft.Create() with
        {
            FirstName = RegistrationField<string>.Captured("Dimas"),
            LastName = RegistrationField<string>.Captured("Tkachenko"),
            Patronymic = RegistrationField<string>.Captured("Petrovych"),
            DateOfBirth = RegistrationField<DateOnly>.Confirmed(new DateOnly(1991, 8, 24)),
            PhoneNumber = RegistrationField<string>.Confirmed("+380501112233"),
            Email = RegistrationField<string>.Confirmed("person@example.com"),
            CurrentRegion = RegistrationField<string>.Confirmed(
                "Kharkivska oblast",
                referenceId: "hfu-region-kharkivska"),
            CurrentCity = RegistrationField<string>.Confirmed("Kharkiv"),
            ActualAddress = RegistrationField<string>.Captured("Main street 1"),
            UserCategory = RegistrationField<UserCategory>.Confirmed(UserCategory.InternallyDisplacedPerson),
            RegionBeforeWar = RegistrationField<string>.Captured(
                "Donetska oblast",
                referenceId: "hfu-region-donetska"),
            DisplacedCertificateYear = RegistrationField<int>.Captured(2022),
            PersonalDataConsent = true,
            RegistrationConfirmed = true
        };

        var dto = FinalRegistrationDtoMapper.Map(draft);

        Assert.Equal("Dimas", dto.FirstName);
        Assert.Equal("Tkachenko", dto.LastName);
        Assert.Equal("Petrovych", dto.Patronymic);
        Assert.Equal(new DateOnly(1991, 8, 24), dto.DateOfBirth);
        Assert.Equal("+380501112233", dto.PhoneNumber);
        Assert.Equal("person@example.com", dto.Email);
        Assert.Equal("Kharkivska oblast", dto.CurrentRegion);
        Assert.Equal("hfu-region-kharkivska", dto.CurrentRegionReferenceId);
        Assert.Equal("Kharkiv", dto.CurrentCity);
        Assert.Equal("Main street 1", dto.ActualAddress);
        Assert.Equal(UserCategory.InternallyDisplacedPerson, dto.UserCategory);
        Assert.Equal("Donetska oblast", dto.RegionBeforeWar);
        Assert.Equal("hfu-region-donetska", dto.RegionBeforeWarReferenceId);
        Assert.Equal(2022, dto.DisplacedCertificateYear);
        Assert.True(dto.PersonalDataConsent);
        Assert.True(dto.RegistrationConfirmed);
    }
}

namespace Hfu.VoiceRegistration.Application.RegistrationCompletion;

public interface IRegistrationIdGenerator
{
    string Generate(DateTimeOffset now);
}

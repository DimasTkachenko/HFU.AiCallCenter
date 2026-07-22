using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Domain.Conversations;

namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationCompletionDetails(
    FinalRegistrationDto FinalRegistration,
    RegistrationResult RegistrationResult);

using Hfu.VoiceRegistration.Application.RegistrationTools;
using Hfu.VoiceRegistration.Application.ReferenceData;
using Hfu.VoiceRegistration.Domain.Conversations;

namespace Hfu.VoiceRegistration.Api.Contracts;

public static class ApiContractMapper
{
    public static ConversationSessionResponse ToResponse(
        ConversationSession session,
        RegistrationStateSnapshot state)
    {
        return new ConversationSessionResponse(
            session.SessionId,
            session.Status,
            session.Version,
            session.CreatedAt,
            session.LastActivityAt,
            session.RealtimeConnectionId,
            session.RegistrationResult,
            state,
            session.Events.Select(ToResponse).ToArray());
    }

    public static RegionsResponse ToResponse(
        IReadOnlyList<RegionReferenceItem> regions)
    {
        return new RegionsResponse(
            regions
                .Select(region => new RegionResponse(
                    region.Id,
                    region.Name,
                    region.Aliases))
                .ToArray());
    }

    private static ConversationEventResponse ToResponse(
        ConversationEvent conversationEvent)
    {
        return new ConversationEventResponse(
            conversationEvent.Type,
            conversationEvent.Message,
            conversationEvent.OccurredAt);
    }
}

using Microsoft.AspNetCore.SignalR;

namespace Hfu.VoiceRegistration.Api.Realtime;

public sealed class ConversationHub : Hub<IConversationRealtimeClient>
{
    public Task JoinSession(Guid sessionId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            ConversationHubGroups.ForSession(sessionId));
    }

    public Task LeaveSession(Guid sessionId)
    {
        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ConversationHubGroups.ForSession(sessionId));
    }
}

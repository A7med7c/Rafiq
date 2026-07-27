using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.Notifications
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<NotificationHub> hubContext,
            IConnectionManager connectionManager,
            ILogger<SignalRNotificationService> logger)
        {
            _hubContext = hubContext;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public async Task SendNotificationToUserAsync(
            string userId,
            string title,
            string message,
            CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.User(userId).SendAsync(
                "ReceiveNotification",
                title,
                message,
                cancellationToken);
        }

        public async Task SendMedicationReminderAsync(
            string userId,
            MedicationReminderNotificationPayload payload,
            CancellationToken cancellationToken = default)
        {
            var connections = _connectionManager.GetConnections(userId);
            var connectionList = connections as System.Collections.Generic.IReadOnlyList<string>
                                 ?? new System.Collections.Generic.List<string>(connections);

            _logger.LogInformation(
                "SendMedicationReminderAsync: userId={UserId}, connectionsFound={Count}, knownUserIds=[{Known}]",
                userId,
                connectionList.Count,
                string.Join(", ", _connectionManager.GetTrackedUserIds()));

            if (connectionList.Count == 0)
            {
                _logger.LogWarning(
                    "No SignalR connections registered for userId={UserId}. MedicationReminderDue NOT sent.",
                    userId);
                return;
            }

            await _hubContext.Clients.Clients(connectionList).SendAsync(
                "MedicationReminderDue",
                payload,
                cancellationToken);

            _logger.LogInformation(
                "MedicationReminderDue sent to userId={UserId} over {Count} connection(s).",
                userId, connectionList.Count);
        }

        public async Task SendAppointmentReminderAsync(
            string userId,
            AppointmentReminderNotificationPayload payload,
            CancellationToken cancellationToken = default)
        {
            var connections = _connectionManager.GetConnections(userId);
            var connectionList = connections as System.Collections.Generic.IReadOnlyList<string>
                                 ?? new System.Collections.Generic.List<string>(connections);

            if (connectionList.Count == 0)
            {
                _logger.LogWarning(
                    "No SignalR connections registered for userId={UserId}. AppointmentReminderDue NOT sent.",
                    userId);
                return;
            }

            await _hubContext.Clients.Clients(connectionList).SendAsync(
                "AppointmentReminderDue",
                payload,
                cancellationToken);

            _logger.LogInformation(
                "AppointmentReminderDue sent to userId={UserId} over {Count} connection(s).",
                userId, connectionList.Count);
        }

        public async Task SendVoiceAgentThinkingAsync(
            string userId,
            VoiceAgentThinkingPayload payload,
            CancellationToken cancellationToken = default)
        {
            var connectionList = GetConnectionList(userId);
            if (connectionList.Count == 0) return;

            await _hubContext.Clients.Clients(connectionList).SendAsync(
                "VoiceAgentThinking",
                payload,
                cancellationToken);
        }

        public async Task SendVoiceAgentResponseAsync(
            string userId,
            VoiceAgentResponsePayload payload,
            CancellationToken cancellationToken = default)
        {
            var connectionList = GetConnectionList(userId);
            if (connectionList.Count == 0)
            {
                _logger.LogWarning(
                    "No SignalR connections for userId={UserId}. VoiceAgentResponse NOT delivered.", userId);
                return;
            }

            await _hubContext.Clients.Clients(connectionList).SendAsync(
                "VoiceAgentResponse",
                payload,
                cancellationToken);
        }

        public async Task SendVoiceAgentErrorAsync(
            string userId,
            VoiceAgentErrorPayload payload,
            CancellationToken cancellationToken = default)
        {
            var connectionList = GetConnectionList(userId);
            if (connectionList.Count == 0) return;

            await _hubContext.Clients.Clients(connectionList).SendAsync(
                "VoiceAgentError",
                payload,
                cancellationToken);
        }

        public async Task SendChatResponseAsync(
            string userId,
            ChatResponsePayload payload,
            CancellationToken cancellationToken = default)
        {
            var connectionList = GetConnectionList(userId);
            if (connectionList.Count == 0)
            {
                _logger.LogWarning(
                    "No SignalR connections for userId={UserId}. ChatResponse NOT delivered for msg {MsgId}.",
                    userId, payload.AssistantMessageId);
                return;
            }

            await _hubContext.Clients.Clients(connectionList).SendAsync(
                "ChatResponse", payload, cancellationToken);
        }

        public async Task SendChatErrorAsync(
            string userId,
            ChatErrorPayload payload,
            CancellationToken cancellationToken = default)
        {
            var connectionList = GetConnectionList(userId);
            if (connectionList.Count == 0) return;

            await _hubContext.Clients.Clients(connectionList).SendAsync(
                "ChatError", payload, cancellationToken);
        }

        private System.Collections.Generic.IReadOnlyList<string> GetConnectionList(string userId)
        {
            var connections = _connectionManager.GetConnections(userId);
            return connections as System.Collections.Generic.IReadOnlyList<string>
                   ?? new System.Collections.Generic.List<string>(connections);
        }
    }
}

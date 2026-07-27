using System.Threading;
using System.Threading.Tasks;

namespace Rafiq.Application.Common.Interfaces
{
    public class MedicationReminderNotificationPayload
    {
        public string ReminderId { get; set; } = string.Empty;
        public string MedicineId { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string ReminderTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string NotificationText { get; set; } = string.Empty;
    }

    public class AppointmentReminderNotificationPayload
    {
        public string AppointmentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string AppointmentDateTime { get; set; } = string.Empty;
        public string NotificationText { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public string? CustomType { get; set; }
    }

    // ── Voice Agent SignalR payloads ──────────────────────────────────────────

    public class VoiceAgentThinkingPayload
    {
        /// <summary>Snake_case name of the tool being executed.</summary>
        public string ToolName { get; set; } = string.Empty;
    }

    public class VoiceAgentResponsePayload
    {
        public Guid SessionId { get; set; }
        /// <summary>Final answer text or follow-up question.</summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>Optional deep-link route to navigate to.</summary>
        public string? NavigateTo { get; set; }
        /// <summary>True when the agent asked a follow-up question instead of completing.</summary>
        public bool NeedsMoreInfo { get; set; }
    }

    public class VoiceAgentErrorPayload
    {
        public string Message { get; set; } = string.Empty;
    }

    public interface INotificationService
    {
        Task SendNotificationToUserAsync(
            string userId,
            string title,
            string message,
            CancellationToken cancellationToken = default);

        Task SendMedicationReminderAsync(
            string userId,
            MedicationReminderNotificationPayload payload,
            CancellationToken cancellationToken = default);

        Task SendAppointmentReminderAsync(
            string userId,
            AppointmentReminderNotificationPayload payload,
            CancellationToken cancellationToken = default);

        // ── Voice Agent events ────────────────────────────────────────────────

        /// <summary>Emitted before each tool call so the UI can show a spinner.</summary>
        Task SendVoiceAgentThinkingAsync(
            string userId,
            VoiceAgentThinkingPayload payload,
            CancellationToken cancellationToken = default);

        /// <summary>Emitted when the agent produces a final answer or follow-up question.</summary>
        Task SendVoiceAgentResponseAsync(
            string userId,
            VoiceAgentResponsePayload payload,
            CancellationToken cancellationToken = default);

        /// <summary>Emitted when the agent fails with an unrecoverable error.</summary>
        Task SendVoiceAgentErrorAsync(
            string userId,
            VoiceAgentErrorPayload payload,
            CancellationToken cancellationToken = default);

        // ── Chat async events ─────────────────────────────────────────────────

        /// <summary>Emitted when the background chat processor completes an assistant message.</summary>
        Task SendChatResponseAsync(
            string userId,
            ChatResponsePayload payload,
            CancellationToken cancellationToken = default);

        /// <summary>Emitted when the background chat processor fails to produce an answer.</summary>
        Task SendChatErrorAsync(
            string userId,
            ChatErrorPayload payload,
            CancellationToken cancellationToken = default);
    }

    public class ChatResponsePayload
    {
        public Guid ConversationId { get; set; }
        public Guid AssistantMessageId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class ChatErrorPayload
    {
        public Guid ConversationId { get; set; }
        public Guid AssistantMessageId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

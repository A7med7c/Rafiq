using MediatR;
using Rafiq.Application.Features.MedicineReminders.Commands.CreateMedicineReminders;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class AddMedicationReminderTool(ISender mediator) : IVoiceTool
{
    public string Name => "add_medication_reminder";
    public string Description => "Sets up daily/weekly/one-time reminder notifications for a medication.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"userMedicineId\",\"startDate\",\"endDate\",\"repeatType\",\"times\"]," +
        "\"properties\":{" +
        "\"userMedicineId\":{\"type\":\"string\",\"format\":\"uuid\"}," +
        "\"startDate\":{\"type\":\"string\",\"format\":\"date\"}," +
        "\"endDate\":{\"type\":\"string\",\"format\":\"date\"}," +
        "\"repeatType\":{\"type\":\"string\",\"enum\":[\"Once\",\"Daily\",\"Weekly\",\"Monthly\"]}," +
        "\"times\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"minItems\":1}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "REQUIRED — never invent; ask the user for each one:\n" +
        "  userMedicineId: UUID of the medication (from add_medication result or list_medications).\n" +
        "  startDate:      When to start (YYYY-MM-DD). Must be today or later.\n" +
        "  repeatType:     One of: Once, Daily, Weekly, Monthly.\n" +
        "                  Infer from context: 'every day' → Daily, 'once' → Once.\n\n" +
        "DEFAULTABLE — ask at most once; if user declines / says 'any time' / 'choose for me' → use default:\n" +
        "  times:     List of HH:mm times. Default if unspecified: [\"08:00\"].\n" +
        "             Convert natural times: '8 AM' → '08:00', '8 PM' → '20:00'.\n" +
        "             If startDate is today, times must be later than NOW (UTC).\n" +
        "  endDate:   When to stop (YYYY-MM-DD). Must be >= startDate.\n" +
        "             Default: match the medication's duration if known; else use 2099-12-31 for ongoing.\n" +
        "             For repeatType 'Once', endDate must equal startDate.\n\n" +
        "BACKEND ENFORCES (do not pre-validate yourself — just call the tool; relay any error returned):\n" +
        "  - Duplicate reminder (same time/startDate/endDate/repeatType) not allowed.\n" +
        "  - Times must be valid HH:mm strings.\n\n" +
        "Multiple reminders can be created in one call by passing multiple times in the array.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        if (!Guid.TryParse(p.GetString("userMedicineId"), out var medicineId))
            return new ToolResult(false, "Invalid medication id.");

        if (!DateOnly.TryParse(p.GetString("startDate"), out var startDate))
            return new ToolResult(false, "Invalid startDate. Use format YYYY-MM-DD.");

        if (!DateOnly.TryParse(p.GetString("endDate"), out var endDate))
            return new ToolResult(false, "Invalid endDate. Use format YYYY-MM-DD.");

        if (!Enum.TryParse<RepeatType>(p.GetString("repeatType"), ignoreCase: true, out var repeatType))
            return new ToolResult(false, "Invalid repeatType. Must be Once, Daily, Weekly, or Monthly.");

        var timesElement = p.GetProperty("times");
        var times = new List<string>();
        foreach (var t in timesElement.EnumerateArray())
        {
            var s = t.GetString();
            if (!string.IsNullOrWhiteSpace(s)) times.Add(s);
        }

        if (times.Count == 0)
            return new ToolResult(false, "At least one reminder time is required.");

        try
        {
            var result = await mediator.Send(
                new CreateMedicineRemindersCommand(medicineId, startDate, endDate, repeatType, times),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to create reminder.");

            var count = result.Data?.Count ?? 0;
            return new ToolResult(true, $"{count} reminder(s) set up successfully.", result.Data);
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "VALIDATION_ERROR");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}

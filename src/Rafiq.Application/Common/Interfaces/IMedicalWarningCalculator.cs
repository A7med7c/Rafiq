using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Interfaces;

public interface IMedicalWarningCalculator
{
    bool RequiresMedicalAttention(double? confidenceScore);
    AttentionLevel ComputeAttentionLevel(double? confidenceScore);
}

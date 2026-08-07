using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Services;

public class MedicalWarningCalculator : IMedicalWarningCalculator
{
    private readonly MedicalWarningSettings _settings;

    public MedicalWarningCalculator(IOptions<MedicalWarningSettings> options)
    {
        _settings = options.Value;
    }

    public bool RequiresMedicalAttention(double? confidenceScore)
    {
        if (!confidenceScore.HasValue) return false;
        return confidenceScore.Value >= _settings.ConfidenceThreshold;
    }

    public AttentionLevel ComputeAttentionLevel(double? confidenceScore)
    {
        if (!confidenceScore.HasValue || confidenceScore.Value < _settings.ConfidenceThreshold)
        {
            return AttentionLevel.Routine;
        }

        if (confidenceScore.Value >= 0.95) return AttentionLevel.Emergency;
        if (confidenceScore.Value >= 0.85) return AttentionLevel.Urgent;
        return AttentionLevel.Soon;
    }
}

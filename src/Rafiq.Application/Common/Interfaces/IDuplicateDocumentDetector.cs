using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Common.Interfaces;

public interface IDuplicateDocumentDetector
{
    /// <summary>
    /// Computes the SHA256 hash of the file bytes and checks for duplicates across all document types.
    /// </summary>
    Task<DuplicateCheckResult> ComputeHashAndCheckAsync(
        byte[] fileBytes,
        string imageUrl,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}

public class DuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    
    public bool IsSameProfile { get; set; }
    
    public string? ExistingProfileId { get; set; }
    
    public string? ExistingProfileName { get; set; }
    
    public string? FileHash { get; set; }
}

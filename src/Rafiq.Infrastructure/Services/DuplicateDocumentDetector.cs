using System.Security.Cryptography;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services;

public class DuplicateDocumentDetector : IDuplicateDocumentDetector
{
    private readonly ILabReportRepository _labReportRepository;
    private readonly IImagingReportRepository _imagingReportRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IUserMedicineRepository _userMedicineRepository;
    private readonly IGeneralDocumentRepository _generalDocumentRepository;
    private readonly IDocumentUploadSessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DuplicateDocumentDetector(
        ILabReportRepository labReportRepository,
        IImagingReportRepository imagingReportRepository,
        IPrescriptionRepository prescriptionRepository,
        IUserMedicineRepository userMedicineRepository,
        IGeneralDocumentRepository generalDocumentRepository,
        IDocumentUploadSessionRepository sessionRepository,
        IUnitOfWork unitOfWork)
    {
        _labReportRepository = labReportRepository;
        _imagingReportRepository = imagingReportRepository;
        _prescriptionRepository = prescriptionRepository;
        _userMedicineRepository = userMedicineRepository;
        _generalDocumentRepository = generalDocumentRepository;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DuplicateCheckResult> ComputeHashAndCheckAsync(
        byte[] fileBytes,
        string imageUrl,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        string fileHash;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(fileBytes);
            fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Check all 5 repositories for the hash
        var result = await CheckAcrossAllTypesAsync(fileHash, currentProfileId, currentUserId, cancellationToken);
        
        if (result != null)
        {
            return new DuplicateCheckResult
            {
                IsDuplicate = true,
                IsSameProfile = result.Value.isSameProfile,
                ExistingProfileId = result.Value.profileId.ToString(),
                ExistingProfileName = result.Value.profileName,
                FileHash = fileHash
            };
        }

        // Cache the hash in DocumentUploadSession (expires in 24 hours)
        var session = new DocumentUploadSession(
            currentUserId,
            imageUrl,
            fileHash,
            DateTime.UtcNow.AddHours(24));
            
        await _sessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DuplicateCheckResult
        {
            IsDuplicate = false,
            FileHash = fileHash
        };
    }

    private async Task<(Guid profileId, string profileName, bool isSameProfile)?> CheckAcrossAllTypesAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var result = await _labReportRepository.FindDuplicateByHashAsync(fileHash, currentProfileId, currentUserId, cancellationToken);
        if (result != null) return result;

        result = await _imagingReportRepository.FindDuplicateByHashAsync(fileHash, currentProfileId, currentUserId, cancellationToken);
        if (result != null) return result;

        result = await _prescriptionRepository.FindDuplicateByHashAsync(fileHash, currentProfileId, currentUserId, cancellationToken);
        if (result != null) return result;

        result = await _userMedicineRepository.FindDuplicateByHashAsync(fileHash, currentProfileId, currentUserId, cancellationToken);
        if (result != null) return result;

        result = await _generalDocumentRepository.FindDuplicateByHashAsync(fileHash, currentProfileId, currentUserId, cancellationToken);
        return result;
    }
}

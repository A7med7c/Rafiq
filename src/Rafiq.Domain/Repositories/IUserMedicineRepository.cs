using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IUserMedicineRepository
{
    Task AddAsync(UserMedicine userMedicine, CancellationToken cancellationToken = default);

    Task<UserMedicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when a non-deleted medicine with the same name and dosage already
    /// exists for the profile.  Comparison is case-insensitive and trims whitespace.
    /// Pass <paramref name="excludeId"/> to skip the medicine being updated.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        Guid userHealthProfileId,
        string medicineName,
        string dosage,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserMedicine>> GetAllByProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);
    
    Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    void Update(UserMedicine userMedicine);

    void Delete(UserMedicine userMedicine);
}

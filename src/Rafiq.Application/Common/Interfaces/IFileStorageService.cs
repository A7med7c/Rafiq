namespace Rafiq.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file stream and returns the relative path or URL.
    /// </summary>
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string folderName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}

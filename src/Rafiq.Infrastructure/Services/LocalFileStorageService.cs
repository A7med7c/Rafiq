using Microsoft.AspNetCore.Hosting;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string folderName,
        CancellationToken cancellationToken = default)
    {
        var wwwRootPath = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(wwwRootPath))
        {
            wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var relativeFolder = Path.Combine("uploads", folderName);
        var absoluteFolder = Path.Combine(wwwRootPath, relativeFolder);

        if (!Directory.Exists(absoluteFolder))
        {
            Directory.CreateDirectory(absoluteFolder);
        }

        var absoluteFilePath = Path.Combine(absoluteFolder, fileName);

        using (var outputStream = new FileStream(absoluteFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await fileStream.CopyToAsync(outputStream, cancellationToken);
        }

        // Return relative path with forward slashes for web consistency
        return $"/{relativeFolder.Replace('\\', '/')}/{fileName}";
    }

    public async Task<byte[]> GetFileBytesAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var wwwRootPath = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(wwwRootPath))
            wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var relativePath = fileUrl.TrimStart('/');
        var absoluteFilePath = Path.Combine(wwwRootPath, relativePath);

        return await File.ReadAllBytesAsync(absoluteFilePath, cancellationToken);
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return Task.CompletedTask;

        var wwwRootPath = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(wwwRootPath))
        {
            wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var relativePath = fileUrl.TrimStart('/');
        var absoluteFilePath = Path.Combine(wwwRootPath, relativePath);

        if (File.Exists(absoluteFilePath))
        {
            File.Delete(absoluteFilePath);
        }

        return Task.CompletedTask;
    }
}

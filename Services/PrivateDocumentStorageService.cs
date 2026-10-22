
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace Staybnb.Services;

public interface IPrivateDocumentStorageService
{
    Task<string?> ValidateAsync(IFormFile file);

    Task<string> SaveAsync(IFormFile file);

    Task<Stream> OpenReadAsync(string storedFileName);

    // Deletes a previously stored private document.
    Task DeleteAsync(string storedFileName);
}

public class PrivateDocumentStorageService
    : IPrivateDocumentStorageService
{
    private readonly string _storagePath;

    private const long MaxDocumentBytes = 5 * 1024 * 1024;

    public PrivateDocumentStorageService(
        IWebHostEnvironment environment)
    {
        // This folder is OUTSIDE wwwroot.
        _storagePath = Path.Combine(
            environment.ContentRootPath,
            "PrivateUploads",
            "GuestDocuments");
    }

    public async Task<string?> ValidateAsync(IFormFile file)
    {
        if (file.Length == 0)
            return "The document is empty.";

        if (file.Length > MaxDocumentBytes)
            return "The document must not exceed 5 MB.";

        var extension = Path.GetExtension(
            file.FileName).ToLowerInvariant();

        if (extension != ".jpg" &&
            extension != ".jpeg" &&
            extension != ".png" &&
            extension != ".pdf")
        {
            return "Only JPG, PNG and PDF files are allowed.";
        }

        var header = new byte[8];

        await using var stream = file.OpenReadStream();

        var bytesRead = await stream.ReadAsync(header);

        var isJpeg = bytesRead >= 3 &&
                     header[0] == 0xFF &&
                     header[1] == 0xD8 &&
                     header[2] == 0xFF;

        var isPng = bytesRead >= 8 &&
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A;

        var isPdf = bytesRead >= 5 &&
                    header[0] == 0x25 &&
                    header[1] == 0x50 &&
                    header[2] == 0x44 &&
                    header[3] == 0x46 &&
                    header[4] == 0x2D;

        if (extension == ".pdf" && !isPdf)
            return "The file is not a valid PDF.";

        if ((extension == ".jpg" || extension == ".jpeg") &&
            !isJpeg)
            return "The file is not a valid JPEG image.";

        if (extension == ".png" && !isPng)
            return "The file is not a valid PNG image.";

        return null;
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        Directory.CreateDirectory(_storagePath);

        var extension = Path.GetExtension(
            file.FileName).ToLowerInvariant();

        var storedFileName =
            $"{Guid.NewGuid():N}{extension}";

        var fullPath = Path.Combine(
            _storagePath, storedFileName);

        await using var destination = new FileStream(
            fullPath, FileMode.CreateNew);

        await file.CopyToAsync(destination);

        return storedFileName;
    }

    public Task<Stream> OpenReadAsync(string storedFileName)
    {
        // Accept only server-generated file names.
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            storedFileName != Path.GetFileName(storedFileName) ||
            !Guid.TryParseExact(
                Path.GetFileNameWithoutExtension(storedFileName),
                "N",
                out _) ||
            !new[] { ".jpg", ".jpeg", ".png", ".pdf" }
                .Contains(Path.GetExtension(storedFileName)
                    .ToLowerInvariant()))
        {
            throw new FileNotFoundException(
                "Invalid document reference.");
        }

        var fullPath = Path.Combine(
            _storagePath, storedFileName);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName)
    {
        // Accept only server-generated file names.
        // This prevents deleting files outside the
        // private document storage folder.
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            storedFileName != Path.GetFileName(storedFileName) ||
            !Guid.TryParseExact(
                Path.GetFileNameWithoutExtension(storedFileName),
                "N",
                out _) ||
            !new[] { ".jpg", ".jpeg", ".png", ".pdf" }
                .Contains(Path.GetExtension(storedFileName)
                    .ToLowerInvariant()))
        {
            throw new FileNotFoundException(
                "Invalid document reference.");
        }

        var fullPath = Path.Combine(
            _storagePath, storedFileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}

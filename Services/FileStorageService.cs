namespace Staybnb.Services;

public static class UploadRules
{
    public const int MaxImages = 8;
    public const long MaxImageBytes = 5 * 1024 * 1024;           // 5 MB per image
    public const long MaxRequestBytes = 50 * 1024 * 1024;        // whole form post
    public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
}

public interface IFileStorageService
{
    /// <summary>Returns an error message, or null if the file is a valid image.</summary>
    Task<string?> ValidateImageAsync(IFormFile file);

    /// <summary>Saves the file under wwwroot/uploads/{subFolder} and returns its web path.</summary>
    Task<string> SaveImageAsync(IFormFile file, string subFolder);

    void Delete(string? webPath);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string?> ValidateImageAsync(IFormFile file)
    {
        if (file.Length == 0)
            return $"'{file.FileName}' is empty.";

        if (file.Length > UploadRules.MaxImageBytes)
            return $"'{file.FileName}' is larger than {UploadRules.MaxImageBytes / 1024 / 1024} MB.";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!UploadRules.ImageExtensions.Contains(extension))
            return $"'{file.FileName}' is not a JPG, PNG or WEBP image.";

        // Check the file's real content, not just its name
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));

        var isJpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = read >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isWebp = read >= 12
                     && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46   // RIFF
                     && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50; // WEBP

        return isJpeg || isPng || isWebp
            ? null
            : $"'{file.FileName}' does not look like a real image file.";
    }

    public async Task<string> SaveImageAsync(IFormFile file, string subFolder)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(folder);

        // A random file name stops clashes and keeps user-supplied names off the disk
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using var target = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(target);

        return $"/uploads/{subFolder}/{fileName}";
    }

    public void Delete(string? webPath)
    {
        // Only ever delete inside wwwroot/uploads (never the demo placeholders or anything else)
        if (string.IsNullOrWhiteSpace(webPath) || !webPath.StartsWith("/uploads/"))
            return;

        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
        var fullPath = Path.GetFullPath(
            Path.Combine(_env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        if (fullPath.StartsWith(uploadsRoot) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
using Microsoft.AspNetCore.Http;

namespace EgitimPlatform.Modules.Content.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<(Stream stream, string contentType, string fileName)> GetFileAsync(string relativePath, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _baseStoragePath;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".xlsx", ".pptx", ".png", ".jpg", ".jpeg"
    };

    public const long MaxFileSize = 25 * 1024 * 1024; // 25 MB

    // Magic numbers (file signatures) to prevent extension spoofing
    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }, // %PDF
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // PK.. (ZIP)
        { ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".pptx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".doc", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } }
    };

    public LocalFileStorageService(string? baseStoragePath = null)
    {
        _baseStoragePath = baseStoragePath ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Materials");
        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Dosya yüklenemedi veya boş.", nameof(file));
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException($"Dosya boyutu 25MB sınırını aşamaz ({file.Length / (1024 * 1024.0):F1} MB).");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Geçersiz dosya uzantısı '{extension}'. Sadece PDF, Word, Excel, PowerPoint ve görsel dosyalarına izin verilir.");
        }

        // Verify file signature (magic bytes)
        if (FileSignatures.TryGetValue(extension, out var signatures))
        {
            using var reader = new BinaryReader(file.OpenReadStream());
            var headerBytes = reader.ReadBytes(8);
            var matchesSignature = signatures.Any(sig => headerBytes.Take(sig.Length).SequenceEqual(sig));
            if (!matchesSignature)
            {
                throw new InvalidOperationException($"Dosya içeriği '{extension}' uzantısı ile uyuşmuyor (sahte dosya tipi).");
            }
        }

        var folderPath = Path.Combine(_baseStoragePath, folder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Safe unique file name: guid + clean extension
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folderPath, safeFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        // Return relative path from base storage
        return Path.Combine(folder, safeFileName).Replace('\\', '/');
    }

    public Task<(Stream stream, string contentType, string fileName)> GetFileAsync(string relativePath, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        // Prevent path traversal
        var sanitizedRelative = relativePath.Replace("..", "").TrimStart('/', '\\');
        var fullPath = Path.Combine(_baseStoragePath, sanitizedRelative);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("İstenen materyal dosyası sunucuda bulunamadı.");
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(((Stream)stream, contentType, originalFileName));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var sanitizedRelative = relativePath.Replace("..", "").TrimStart('/', '\\');
        var fullPath = Path.Combine(_baseStoragePath, sanitizedRelative);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}

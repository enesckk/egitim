using System.Security;
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
    private readonly string _baseStoragePathWithSeparator;
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
        var resolvedBase = baseStoragePath ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Materials");
        _baseStoragePath = Path.GetFullPath(resolvedBase);
        _baseStoragePathWithSeparator = _baseStoragePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
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

        var normalizedFileName = Path.GetFileName((file.FileName ?? string.Empty).Replace('\\', '/'));
        var extension = Path.GetExtension(normalizedFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Geçersiz dosya uzantısı '{extension}'. Sadece PDF, Word, Excel, PowerPoint ve görsel dosyalarına izin verilir.");
        }

        // Verify file signature (magic bytes)
        if (FileSignatures.TryGetValue(extension, out var signatures))
        {
            await using var headerStream = file.OpenReadStream();
            if (headerStream.CanSeek)
            {
                headerStream.Position = 0;
            }

            var headerBytes = new byte[8];
            var bytesRead = await headerStream.ReadAsync(headerBytes.AsMemory(0, 8), cancellationToken);
            var matchesSignature = signatures.Any(sig =>
                bytesRead >= sig.Length && headerBytes.AsSpan(0, sig.Length).SequenceEqual(sig));
            if (!matchesSignature)
            {
                throw new InvalidOperationException($"Dosya içeriği '{extension}' uzantısı ile uyuşmuyor (sahte dosya tipi).");
            }

            if (headerStream.CanSeek)
            {
                headerStream.Position = 0;
            }
        }

        var sanitizedFolder = SanitizeRelativePath(folder, out var folderFullPath);
        if (!Directory.Exists(folderFullPath))
        {
            Directory.CreateDirectory(folderFullPath);
        }

        // Safe unique file name: guid + clean extension
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.GetFullPath(Path.Combine(folderFullPath, safeFileName));
        EnsureWithinBaseStorage(fullPath);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        // Return relative path from base storage
        return Path.Combine(sanitizedFolder, safeFileName).Replace('\\', '/');
    }

    public Task<(Stream stream, string contentType, string fileName)> GetFileAsync(string relativePath, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        SanitizeRelativePath(relativePath, out var fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("İstenen materyal dosyası sunucuda bulunamadı.");
        }

        var safeDownloadName = Path.GetFileName((originalFileName ?? string.Empty).Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(safeDownloadName))
        {
            safeDownloadName = Path.GetFileName(fullPath);
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(((Stream)stream, contentType, safeDownloadName));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        SanitizeRelativePath(relativePath, out var fullPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string SanitizeRelativePath(string relativePath, out string fullPath)
    {
        var normalized = (relativePath ?? string.Empty).Replace('\\', '/');
        var safeSegments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => segment.Replace("..", string.Empty))
            .Where(segment => !string.IsNullOrWhiteSpace(segment) && segment != ".")
            .ToArray();

        var sanitizedRelative = safeSegments.Length > 0
            ? Path.Combine(safeSegments)
            : string.Empty;

        fullPath = Path.GetFullPath(Path.Combine(_baseStoragePath, sanitizedRelative));
        EnsureWithinBaseStorage(fullPath);
        return sanitizedRelative;
    }

    private void EnsureWithinBaseStorage(string fullPath)
    {
        if (!string.Equals(fullPath, _baseStoragePath, StringComparison.Ordinal) &&
            !fullPath.StartsWith(_baseStoragePathWithSeparator, StringComparison.Ordinal))
        {
            throw new SecurityException("Dosya yolu güvenli depolama dizini dışına çıkamaz.");
        }
    }
}

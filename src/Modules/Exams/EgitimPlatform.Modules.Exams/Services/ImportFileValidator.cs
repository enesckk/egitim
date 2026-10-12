using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace EgitimPlatform.Modules.Exams.Services;

public interface IImportFileValidator
{
    void ValidateFileHeader(IFormFile? file);
    IXLWorkbook OpenWorkbook(IFormFile file);
}

public class ImportFileValidator : IImportFileValidator
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public void ValidateFileHeader(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("Import file is required and cannot be empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ValidationException($"Import file exceeds maximum allowed size of 5 MB (Current size: {file.Length / (1024 * 1024.0):F2} MB).");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            throw new ValidationException($"Unsupported file format '{extension}'. Only standard Excel workbooks (.xlsx) are allowed.");
        }

        if (!string.IsNullOrEmpty(file.ContentType))
        {
            var contentType = file.ContentType.ToLowerInvariant();
            if (contentType.Contains("executable") || contentType.Contains("x-msdownload"))
            {
                throw new ValidationException("File content type indicates an unsupported or unsafe binary file.");
            }
        }
    }

    public IXLWorkbook OpenWorkbook(IFormFile file)
    {
        ValidateFileHeader(file);
        try
        {
            using var stream = file.OpenReadStream();
            return new XLWorkbook(stream);
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            throw new ValidationException($"Failed to read Excel workbook: {ex.Message}");
        }
    }
}

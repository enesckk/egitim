using System.Security;
using System.Text.Json;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Content.Entities;
using EgitimPlatform.Modules.Content.Services;
using EgitimPlatform.Modules.Teachers.Entities;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Content.Features.ManageMaterials;

public interface IMaterialsHandler
{
    Task<List<ContentMaterialItemDto>> GetTeacherMaterialsAsync(CancellationToken cancellationToken = default);
    Task<ContentMaterialItemDto> CreateMaterialAsync(CreateMaterialRequest request, CancellationToken cancellationToken = default);
    Task<bool> AssignToClassroomAsync(Guid materialId, AssignMaterialRequest request, CancellationToken cancellationToken = default);
    Task<bool> RevokeAssignmentAsync(Guid materialId, Guid classroomId, CancellationToken cancellationToken = default);
    Task<List<StudentMaterialDto>> GetStudentMaterialsAsync(CancellationToken cancellationToken = default);
    Task<(Stream stream, string contentType, string fileName)> DownloadMaterialAsync(Guid materialId, CancellationToken cancellationToken = default);
}

public class MaterialsHandler : IMaterialsHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _storageService;
    private readonly IAuditService _auditService;

    public MaterialsHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IFileStorageService storageService,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _storageService = storageService;
        _auditService = auditService;
    }

    public async Task<List<ContentMaterialItemDto>> GetTeacherMaterialsAsync(CancellationToken cancellationToken = default)
    {
        var instId = await ValidateTeacherAccessAsync(cancellationToken);
        var teacher = await GetTeacherEntityAsync(instId, cancellationToken);

        var materials = await _db.Set<ContentMaterial>()
            .Where(m => m.InstitutionId == instId && m.TeacherId == teacher.Id)
            .Include(m => m.Assignments)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var classroomIds = materials.SelectMany(m => m.Assignments).Select(a => a.ClassroomId).Distinct().ToList();
        var classroomNames = await _db.Set<Classroom>()
            .Where(c => c.InstitutionId == instId && classroomIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return materials.Select(m => new ContentMaterialItemDto(
            m.Id,
            m.Title,
            m.Subject,
            m.Topic,
            m.Grade,
            m.MaterialType,
            m.Description,
            m.PageCount,
            m.QuestionCount,
            m.DurationMinutes,
            m.OriginalFileName,
            m.FileSizeBytes,
            m.Assignments.Count(a => a.IsActive),
            m.CreatedAt,
            m.Assignments.Select(a => new ContentMaterialAssignmentDto(
                a.Id,
                a.ClassroomId,
                classroomNames.TryGetValue(a.ClassroomId, out var name) ? name : null,
                a.Note,
                a.IsActive,
                a.CreatedAt
            )).ToList()
        )).ToList();
    }

    public async Task<ContentMaterialItemDto> CreateMaterialAsync(CreateMaterialRequest request, CancellationToken cancellationToken = default)
    {
        var instId = await ValidateTeacherAccessAsync(cancellationToken);
        var teacher = await GetTeacherEntityAsync(instId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Materyal başlığı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ArgumentException("Ders / Branş bilgisi zorunludur.");
        if (request.File == null || request.File.Length == 0)
            throw new ArgumentException("Dosya yüklenmesi zorunludur.");

        var folder = $"inst_{instId:N}/teacher_{teacher.Id:N}";
        var storagePath = await _storageService.SaveFileAsync(request.File, folder, cancellationToken);

        try
        {
            var material = new ContentMaterial
            {
                Id = Guid.NewGuid(),
                InstitutionId = instId,
                TeacherId = teacher.Id,
                Title = request.Title.Trim(),
                Subject = request.Subject.Trim(),
                Topic = request.Topic?.Trim() ?? string.Empty,
                Grade = request.Grade?.Trim() ?? string.Empty,
                MaterialType = request.MaterialType?.Trim() ?? "fasikul",
                Description = request.Description?.Trim(),
                PageCount = request.PageCount,
                QuestionCount = request.QuestionCount,
                DurationMinutes = request.DurationMinutes,
                OriginalFileName = Path.GetFileName((request.File.FileName ?? string.Empty).Replace('\\', '/')),
                StoragePath = storagePath,
                ContentType = request.File.ContentType ?? "application/octet-stream",
                FileSizeBytes = request.File.Length,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            if (request.AssignClassroomIds != null && request.AssignClassroomIds.Count > 0)
            {
                var teacherClassIds = await _db.Set<ClassroomTeacher>()
                    .Where(ct => ct.InstitutionId == instId && ct.TeacherId == teacher.Id)
                    .Select(ct => ct.ClassroomId)
                    .ToListAsync(cancellationToken);

                foreach (var cid in request.AssignClassroomIds.Distinct())
                {
                    if (!teacherClassIds.Contains(cid))
                    {
                        throw new SecurityException("Öğretmen yetkili olmadığı bir sınıfa materyal atayamaz.");
                    }

                    material.Assignments.Add(new ContentMaterialAssignment
                    {
                        Id = Guid.NewGuid(),
                        InstitutionId = instId,
                        MaterialId = material.Id,
                        ClassroomId = cid,
                        IsActive = true,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = _currentUser.UserId
                    });
                }
            }

            _db.Set<ContentMaterial>().Add(material);
            await _db.SaveChangesAsync(cancellationToken);

            var metadataJson = JsonSerializer.Serialize(new
            {
                title = material.Title,
                fileName = material.OriginalFileName
            });

            await _auditService.LogAsync(
                userId: _currentUser.UserId ?? Guid.Empty,
                action: "CreateMaterial",
                entityType: nameof(ContentMaterial),
                entityId: material.Id.ToString(),
                institutionId: instId,
                metadataJson: metadataJson,
                cancellationToken: cancellationToken
            );

            return new ContentMaterialItemDto(
                material.Id,
                material.Title,
                material.Subject,
                material.Topic,
                material.Grade,
                material.MaterialType,
                material.Description,
                material.PageCount,
                material.QuestionCount,
                material.DurationMinutes,
                material.OriginalFileName,
                material.FileSizeBytes,
                material.Assignments.Count(a => a.IsActive),
                material.CreatedAt,
                new List<ContentMaterialAssignmentDto>()
            );
        }
        catch
        {
            await _storageService.DeleteFileAsync(storagePath, CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> AssignToClassroomAsync(Guid materialId, AssignMaterialRequest request, CancellationToken cancellationToken = default)
    {
        var instId = await ValidateTeacherAccessAsync(cancellationToken);
        var teacher = await GetTeacherEntityAsync(instId, cancellationToken);

        var material = await _db.Set<ContentMaterial>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == materialId && m.InstitutionId == instId, cancellationToken);

        if (material == null)
            throw new KeyNotFoundException("Materyal bulunamadı.");

        if (material.TeacherId != teacher.Id)
            throw new SecurityException("Başka bir öğretmene ait materyal üzerinde işlem yapılamaz.");

        var isTeacherInClass = await _db.Set<ClassroomTeacher>()
            .AnyAsync(ct => ct.InstitutionId == instId && ct.TeacherId == teacher.Id && ct.ClassroomId == request.ClassroomId, cancellationToken);

        if (!isTeacherInClass)
            throw new SecurityException("Öğretmen yetkili olmadığı bir sınıfa materyal atayamaz.");

        var existingAssignment = await _db.Set<ContentMaterialAssignment>()
            .FirstOrDefaultAsync(a => a.MaterialId == materialId && a.ClassroomId == request.ClassroomId && a.InstitutionId == instId, cancellationToken);

        if (existingAssignment != null)
        {
            existingAssignment.IsActive = true;
            existingAssignment.Note = request.Note;
            existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
            existingAssignment.UpdatedBy = _currentUser.UserId;
        }
        else
        {
            _db.Set<ContentMaterialAssignment>().Add(new ContentMaterialAssignment
            {
                Id = Guid.NewGuid(),
                InstitutionId = instId,
                MaterialId = materialId,
                ClassroomId = request.ClassroomId,
                Note = request.Note,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = _currentUser.UserId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeAssignmentAsync(Guid materialId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        var instId = await ValidateTeacherAccessAsync(cancellationToken);
        var teacher = await GetTeacherEntityAsync(instId, cancellationToken);

        var material = await _db.Set<ContentMaterial>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == materialId && m.InstitutionId == instId, cancellationToken);

        if (material == null)
            throw new KeyNotFoundException("Materyal bulunamadı.");

        if (material.TeacherId != teacher.Id)
            throw new SecurityException("Başka bir öğretmene ait materyal üzerinde işlem yapılamaz.");

        var assignment = await _db.Set<ContentMaterialAssignment>()
            .FirstOrDefaultAsync(a => a.MaterialId == materialId && a.ClassroomId == classroomId && a.InstitutionId == instId && a.IsActive, cancellationToken);

        if (assignment == null)
            return false;

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        assignment.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<StudentMaterialDto>> GetStudentMaterialsAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.Roles.Contains(Roles.Student))
            throw new SecurityException("Sadece öğrenciler bu materyallere erişebilir.");

        var instId = await _currentUser.GetInstitutionIdAsync()
            ?? throw new SecurityException("Kurum bilgisi bulunamadı.");

        var student = await _db.Set<Student>()
            .FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId && s.InstitutionId == instId, cancellationToken)
            ?? throw new SecurityException("Öğrenci profili bulunamadı.");

        var enrolledClassIds = await _db.Set<ClassroomStudent>()
            .Where(cs => cs.InstitutionId == instId && cs.StudentId == student.Id)
            .Select(cs => cs.ClassroomId)
            .ToListAsync(cancellationToken);

        if (enrolledClassIds.Count == 0)
            return new List<StudentMaterialDto>();

        var classrooms = await _db.Set<Classroom>()
            .Where(c => c.InstitutionId == instId && enrolledClassIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var assignments = await _db.Set<ContentMaterialAssignment>()
            .Where(a => a.InstitutionId == instId && enrolledClassIds.Contains(a.ClassroomId) && a.IsActive)
            .Include(a => a.Material)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<StudentMaterialDto>();
        foreach (var a in assignments)
        {
            if (a.Material == null) continue;
            result.Add(new StudentMaterialDto(
                a.Material.Id,
                a.Material.Title,
                a.Material.Subject,
                a.Material.Topic,
                a.Material.Grade,
                a.Material.MaterialType,
                a.Material.Description,
                a.Material.PageCount,
                a.Material.QuestionCount,
                a.Material.DurationMinutes,
                a.Material.OriginalFileName,
                a.Material.FileSizeBytes,
                classrooms.TryGetValue(a.ClassroomId, out var cName) ? cName : "Sınıf",
                a.Note,
                a.CreatedAt
            ));
        }

        return result;
    }

    public async Task<(Stream stream, string contentType, string fileName)> DownloadMaterialAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            throw new SecurityException("Oturum açılmalıdır.");

        var instId = await _currentUser.GetInstitutionIdAsync()
            ?? throw new SecurityException("Kurum bilgisi bulunamadı.");

        var material = await _db.Set<ContentMaterial>()
            .Include(m => m.Assignments)
            .FirstOrDefaultAsync(m => m.Id == materialId && m.InstitutionId == instId, cancellationToken)
            ?? throw new KeyNotFoundException("Materyal bulunamadı.");

        if (_currentUser.Roles.Contains(Roles.Teacher))
        {
            var teacher = await GetTeacherEntityAsync(instId, cancellationToken);
            if (material.TeacherId != teacher.Id)
            {
                var teacherClassIds = await _db.Set<ClassroomTeacher>()
                    .Where(ct => ct.InstitutionId == instId && ct.TeacherId == teacher.Id)
                    .Select(ct => ct.ClassroomId)
                    .ToListAsync(cancellationToken);

                var hasSharedClass = material.Assignments.Any(a => a.IsActive && teacherClassIds.Contains(a.ClassroomId));
                if (!hasSharedClass)
                {
                    throw new SecurityException("Bu materyali indirme yetkiniz yok.");
                }
            }
        }
        else if (_currentUser.Roles.Contains(Roles.Student))
        {
            var student = await _db.Set<Student>()
                .FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId && s.InstitutionId == instId, cancellationToken)
                ?? throw new SecurityException("Öğrenci profili bulunamadı.");

            var enrolledClassIds = await _db.Set<ClassroomStudent>()
                .Where(cs => cs.InstitutionId == instId && cs.StudentId == student.Id)
                .Select(cs => cs.ClassroomId)
                .ToListAsync(cancellationToken);

            var hasActiveAssignment = material.Assignments.Any(a => a.IsActive && enrolledClassIds.Contains(a.ClassroomId));
            if (!hasActiveAssignment)
            {
                throw new SecurityException("Bu materyale erişim yetkiniz yok veya erişiminiz sonlandırılmış.");
            }
        }
        else if (_currentUser.Roles.Contains(Roles.SuperAdmin) || _currentUser.Roles.Contains(Roles.InstitutionAdmin))
        {
            // Allowed
        }
        else
        {
            throw new SecurityException("Bu rolün materyal indirme yetkisi yoktur.");
        }

        return await _storageService.GetFileAsync(material.StoragePath, material.OriginalFileName, material.ContentType, cancellationToken);
    }

    private async Task<Guid> ValidateTeacherAccessAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.Roles.Contains(Roles.Teacher))
            throw new SecurityException("Bu işlem sadece öğretmenler tarafından gerçekleştirilebilir.");

        return await _currentUser.GetInstitutionIdAsync()
            ?? throw new SecurityException("Kurum bilgisi doğrulanamadı.");
    }

    private async Task<Teacher> GetTeacherEntityAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        var teacher = await _db.Set<Teacher>()
            .FirstOrDefaultAsync(t => t.UserId == _currentUser.UserId && t.InstitutionId == institutionId, cancellationToken);

        if (teacher == null)
            throw new SecurityException("Öğretmen profili bulunamadı.");

        return teacher;
    }
}

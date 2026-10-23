using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Notebook.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Notebook.Features.ManageNotebook;

public sealed class NotebookHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    IAuditService audit)
{
    private async Task<(Guid institutionId, Guid studentId)> ResolveStudentContextAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenException("Kimlik doğrulaması gereklidir.");

        if (!currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.Student))
            throw new ForbiddenException("Öğrenci defterine yalnızca öğrencinin kendisi erişebilir.");

        var userInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !userInstId.HasValue)
            throw new ForbiddenException("Kurum bilgisi gereklidir.");

        var institutionId = userInstId ?? Guid.Empty;

        if (currentUser.IsSuperAdmin)
        {
            var anyStudent = await studentDirectory.GetStudentAsync(Guid.Empty, ct);
            return (institutionId, anyStudent?.Id ?? Guid.Empty);
        }

        var student = await studentDirectory.GetStudentByUserIdAsync(currentUser.UserId.Value, institutionId, ct)
            ?? throw new ForbiddenException("Öğrenci profiliniz bulunamadı.");

        return (institutionId, student.Id);
    }

    public async Task<PaginatedList<NotebookEntryDto>> ListAsync(ListNotebookEntriesQuery query, CancellationToken ct = default)
    {
        await new ListNotebookEntriesValidator().ValidateAndThrowAsync(query, ct);
        var (institutionId, studentId) = await ResolveStudentContextAsync(ct);

        var dbQuery = db.Set<NotebookEntry>()
            .AsNoTracking()
            .Where(n => n.StudentId == studentId && !n.IsDeleted);

        if (!currentUser.IsSuperAdmin)
        {
            dbQuery = dbQuery.Where(n => n.InstitutionId == institutionId);
        }

        if (query.SubjectId.HasValue)
        {
            dbQuery = dbQuery.Where(n => n.SubjectId == query.SubjectId.Value);
        }

        if (query.IsPinned.HasValue)
        {
            dbQuery = dbQuery.Where(n => n.IsPinned == query.IsPinned.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(n => n.Title.ToLower().Contains(search) || n.Content.ToLower().Contains(search));
        }

        var totalCount = await dbQuery.CountAsync(ct);

        var items = await dbQuery
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotebookEntryDto(
                n.Id,
                n.StudentId,
                n.Title,
                n.Content,
                n.SubjectId,
                n.TopicId,
                n.Tags,
                n.IsPinned,
                n.CreatedAt,
                n.UpdatedAt))
            .ToListAsync(ct);

        if (currentUser.IsSuperAdmin && currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "NotebookEntry.SuperAdminRead",
                "Student",
                studentId.ToString(),
                institutionId);
            await db.SaveChangesAsync(ct);
        }

        return new PaginatedList<NotebookEntryDto>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<NotebookEntryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var (institutionId, studentId) = await ResolveStudentContextAsync(ct);

        var entry = await db.Set<NotebookEntry>()
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, ct)
            ?? throw new NotFoundException("NotebookEntry", id);

        if (!currentUser.IsSuperAdmin)
        {
            if (entry.InstitutionId != institutionId || entry.StudentId != studentId)
                throw new NotFoundException("NotebookEntry", id);
        }

        return new NotebookEntryDto(
            entry.Id,
            entry.StudentId,
            entry.Title,
            entry.Content,
            entry.SubjectId,
            entry.TopicId,
            entry.Tags,
            entry.IsPinned,
            entry.CreatedAt,
            entry.UpdatedAt);
    }

    public async Task<NotebookEntryDto> CreateAsync(CreateNotebookEntryCommand command, CancellationToken ct = default)
    {
        await new CreateNotebookEntryValidator().ValidateAndThrowAsync(command, ct);
        var (institutionId, studentId) = await ResolveStudentContextAsync(ct);

        var entry = new NotebookEntry
        {
            Id = Guid.CreateVersion7(),
            InstitutionId = institutionId,
            StudentId = studentId,
            Title = command.Title.Trim(),
            Content = command.Content.Trim(),
            SubjectId = command.SubjectId,
            TopicId = command.TopicId,
            Tags = string.IsNullOrWhiteSpace(command.Tags) ? null : command.Tags.Trim(),
            IsPinned = command.IsPinned,
            CreatedBy = currentUser.UserId,
        };

        db.Set<NotebookEntry>().Add(entry);

        if (currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "NotebookEntry.Created",
                "NotebookEntry",
                entry.Id.ToString(),
                institutionId);
        }

        await db.SaveChangesAsync(ct);

        return new NotebookEntryDto(
            entry.Id,
            entry.StudentId,
            entry.Title,
            entry.Content,
            entry.SubjectId,
            entry.TopicId,
            entry.Tags,
            entry.IsPinned,
            entry.CreatedAt,
            entry.UpdatedAt);
    }

    public async Task<NotebookEntryDto> UpdateAsync(UpdateNotebookEntryCommand command, CancellationToken ct = default)
    {
        await new UpdateNotebookEntryValidator().ValidateAndThrowAsync(command, ct);
        var (institutionId, studentId) = await ResolveStudentContextAsync(ct);

        var entry = await db.Set<NotebookEntry>()
            .FirstOrDefaultAsync(n => n.Id == command.Id && !n.IsDeleted, ct)
            ?? throw new NotFoundException("NotebookEntry", command.Id);

        if (!currentUser.IsSuperAdmin)
        {
            if (entry.InstitutionId != institutionId || entry.StudentId != studentId)
                throw new ForbiddenException("Yalnızca kendi defter notunuzu güncelleyebilirsiniz.");
        }

        entry.Title = command.Title.Trim();
        entry.Content = command.Content.Trim();
        entry.SubjectId = command.SubjectId;
        entry.TopicId = command.TopicId;
        entry.Tags = string.IsNullOrWhiteSpace(command.Tags) ? null : command.Tags.Trim();
        entry.IsPinned = command.IsPinned;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        entry.UpdatedBy = currentUser.UserId;

        if (currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "NotebookEntry.Updated",
                "NotebookEntry",
                entry.Id.ToString(),
                entry.InstitutionId);
        }

        await db.SaveChangesAsync(ct);

        return new NotebookEntryDto(
            entry.Id,
            entry.StudentId,
            entry.Title,
            entry.Content,
            entry.SubjectId,
            entry.TopicId,
            entry.Tags,
            entry.IsPinned,
            entry.CreatedAt,
            entry.UpdatedAt);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var (institutionId, studentId) = await ResolveStudentContextAsync(ct);

        var entry = await db.Set<NotebookEntry>()
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, ct)
            ?? throw new NotFoundException("NotebookEntry", id);

        if (!currentUser.IsSuperAdmin)
        {
            if (entry.InstitutionId != institutionId || entry.StudentId != studentId)
                throw new ForbiddenException("Yalnızca kendi defter notunuzu silebilirsiniz.");
        }

        entry.IsDeleted = true;
        entry.DeletedAt = DateTimeOffset.UtcNow;
        entry.DeletedBy = currentUser.UserId;

        if (currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "NotebookEntry.Deleted",
                "NotebookEntry",
                entry.Id.ToString(),
                entry.InstitutionId);
        }

        await db.SaveChangesAsync(ct);
    }
}

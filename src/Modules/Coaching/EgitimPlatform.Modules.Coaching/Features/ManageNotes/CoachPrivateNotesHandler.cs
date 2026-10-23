using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Features.ManageNotes;

public sealed class CoachPrivateNotesHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAuditService audit)
{
    private async Task<(Guid institutionId, Coach? coach)> ResolveCoachContextAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenException("Kimlik doğrulaması gereklidir.");

        // SECURITY.md: Coach private notes are completely forbidden to InstitutionAdmin, Teacher, Student, Parent.
        if (!currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.Coach))
            throw new ForbiddenException("Koç özel notlarına yalnızca atanmış koç erişebilir.");

        var userInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !userInstId.HasValue)
            throw new ForbiddenException("Kurum bilgisi gereklidir.");

        var institutionId = userInstId ?? Guid.Empty;

        if (currentUser.IsSuperAdmin)
            return (institutionId, null);

        var coach = await db.Set<Coach>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == currentUser.UserId.Value && c.InstitutionId == institutionId && !c.IsDeleted, ct)
            ?? throw new ForbiddenException("Bu kurumda aktif koç profiliniz bulunamadı.");

        return (institutionId, coach);
    }

    private async Task EnsureAssignmentAsync(Guid coachId, Guid studentId, Guid institutionId, CancellationToken ct)
    {
        var isAssigned = await db.Set<StudentCoachAssignment>()
            .AsNoTracking()
            .AnyAsync(a => a.CoachId == coachId
                        && a.StudentId == studentId
                        && a.InstitutionId == institutionId
                        && a.IsActive
                        && a.EndedAt == null, ct);

        if (!isAssigned)
            throw new ForbiddenException("Bu öğrenciye atanmış aktif koçluk yetkiniz bulunmamaktadır.");
    }

    public async Task<IReadOnlyList<CoachPrivateNoteDto>> ListAsync(ListCoachPrivateNotesQuery query, CancellationToken ct = default)
    {
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);

        var notesQuery = db.Set<CoachPrivateNote>()
            .AsNoTracking()
            .Where(n => n.StudentId == query.StudentId && !n.IsDeleted);

        if (!currentUser.IsSuperAdmin)
        {
            notesQuery = notesQuery.Where(n => n.InstitutionId == institutionId);
            if (coach is not null)
            {
                await EnsureAssignmentAsync(coach.Id, query.StudentId, institutionId, ct);
                notesQuery = notesQuery.Where(n => n.CoachId == coach.Id);
            }
        }

        var list = await notesQuery
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new CoachPrivateNoteDto(
                n.Id,
                n.StudentId,
                n.CoachId,
                n.Category,
                n.Content,
                n.CreatedAt,
                n.UpdatedAt))
            .ToListAsync(ct);

        if (currentUser.IsSuperAdmin && currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "CoachPrivateNote.SuperAdminRead",
                "Student",
                query.StudentId.ToString(),
                institutionId);
            await db.SaveChangesAsync(ct);
        }

        return list;
    }

    public async Task<CoachPrivateNoteDto> CreateAsync(CreateCoachPrivateNoteCommand command, CancellationToken ct = default)
    {
        await new CreateCoachPrivateNoteValidator().ValidateAndThrowAsync(command, ct);
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);

        var targetCoachId = coach?.Id ?? Guid.Empty;
        if (currentUser.IsSuperAdmin && targetCoachId == Guid.Empty)
        {
            // For superadmin with explicit mock or test without a coach profile, find any coach or create dummy
            var existingCoach = await db.Set<Coach>().FirstOrDefaultAsync(c => !c.IsDeleted, ct);
            if (existingCoach is not null)
            {
                targetCoachId = existingCoach.Id;
                institutionId = existingCoach.InstitutionId;
            }
            else
            {
                throw new ValidationException("Sistemde kayıtlı koç bulunamadı.");
            }
        }
        else if (coach is not null)
        {
            await EnsureAssignmentAsync(coach.Id, command.StudentId, institutionId, ct);
        }

        var note = new CoachPrivateNote
        {
            Id = Guid.CreateVersion7(),
            InstitutionId = institutionId,
            CoachId = targetCoachId,
            StudentId = command.StudentId,
            Category = command.Category.Trim(),
            Content = command.Content.Trim(),
            CreatedBy = currentUser.UserId,
        };

        db.Set<CoachPrivateNote>().Add(note);

        if (currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "CoachPrivateNote.Created",
                "CoachPrivateNote",
                note.Id.ToString(),
                institutionId);
        }

        await db.SaveChangesAsync(ct);

        return new CoachPrivateNoteDto(
            note.Id,
            note.StudentId,
            note.CoachId,
            note.Category,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);
    }

    public async Task<CoachPrivateNoteDto> UpdateAsync(UpdateCoachPrivateNoteCommand command, CancellationToken ct = default)
    {
        await new UpdateCoachPrivateNoteValidator().ValidateAndThrowAsync(command, ct);
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);

        var note = await db.Set<CoachPrivateNote>()
            .FirstOrDefaultAsync(n => n.Id == command.NoteId && !n.IsDeleted, ct)
            ?? throw new NotFoundException("CoachPrivateNote", command.NoteId);

        if (!currentUser.IsSuperAdmin)
        {
            if (note.InstitutionId != institutionId)
                throw new NotFoundException("CoachPrivateNote", command.NoteId);

            if (coach is not null)
            {
                if (note.CoachId != coach.Id)
                    throw new ForbiddenException("Yalnızca kendi oluşturduğunuz koç özel notunu güncelleyebilirsiniz.");

                await EnsureAssignmentAsync(coach.Id, note.StudentId, institutionId, ct);
            }
        }

        note.Category = command.Category.Trim();
        note.Content = command.Content.Trim();
        note.UpdatedAt = DateTimeOffset.UtcNow;
        note.UpdatedBy = currentUser.UserId;

        if (currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "CoachPrivateNote.Updated",
                "CoachPrivateNote",
                note.Id.ToString(),
                note.InstitutionId);
        }

        await db.SaveChangesAsync(ct);

        return new CoachPrivateNoteDto(
            note.Id,
            note.StudentId,
            note.CoachId,
            note.Category,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);
    }

    public async Task DeleteAsync(Guid noteId, CancellationToken ct = default)
    {
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);

        var note = await db.Set<CoachPrivateNote>()
            .FirstOrDefaultAsync(n => n.Id == noteId && !n.IsDeleted, ct)
            ?? throw new NotFoundException("CoachPrivateNote", noteId);

        if (!currentUser.IsSuperAdmin)
        {
            if (note.InstitutionId != institutionId)
                throw new NotFoundException("CoachPrivateNote", noteId);

            if (coach is not null)
            {
                if (note.CoachId != coach.Id)
                    throw new ForbiddenException("Yalnızca kendi oluşturduğunuz koç özel notunu silebilirsiniz.");

                await EnsureAssignmentAsync(coach.Id, note.StudentId, institutionId, ct);
            }
        }

        note.IsDeleted = true;
        note.DeletedAt = DateTimeOffset.UtcNow;
        note.DeletedBy = currentUser.UserId;

        if (currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                currentUser.UserId.Value,
                "CoachPrivateNote.Deleted",
                "CoachPrivateNote",
                note.Id.ToString(),
                note.InstitutionId);
        }

        await db.SaveChangesAsync(ct);
    }
}

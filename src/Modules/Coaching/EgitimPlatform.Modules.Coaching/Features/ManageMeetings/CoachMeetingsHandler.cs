using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Features.ManageMeetings;

public sealed class CoachMeetingsHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAuditService audit,
    INotificationService? notificationService = null)
{
    private async Task<(Guid institutionId, Coach? coach)> ResolveCoachContextAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenException("Kimlik doğrulaması gereklidir.");

        if (!currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.Coach))
            throw new ForbiddenException("Görüşmeleri yalnızca atanmış koç yönetebilir.");

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
            throw new ForbiddenException("Öğrenci aktif olarak bu koça atanmamış veya ataması sonlandırılmış.");
    }

    public async Task<IReadOnlyList<CoachMeetingDto>> ListAsync(CancellationToken ct = default)
    {
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);

        var query = db.Set<CoachMeeting>().AsNoTracking()
            .Where(m => m.InstitutionId == institutionId && !m.IsDeleted);

        if (coach != null)
        {
            query = query.Where(m => m.CoachId == coach.Id);
        }

        var meetings = await query
            .OrderByDescending(m => m.StartTime)
            .ToListAsync(ct);

        if (meetings.Count == 0)
            return [];

        // Query student names & grade levels
        var studentIds = meetings.Select(m => m.StudentId).Distinct().ToList();
        var studentDetails = new Dictionary<Guid, (string Name, string Grade)>();

        if (db is Microsoft.EntityFrameworkCore.DbContext efDb && efDb.Database.IsRelational())
        {
            using var cmd = efDb.Database.GetDbConnection().CreateCommand();
            var idList = string.Join(",", studentIds.Select(id => $"'{id}'"));
            cmd.CommandText = $"SELECT Id, FirstName, LastName, GradeLevel FROM Students WHERE Id IN ({idList}) AND InstitutionId = '{institutionId}' AND IsDeleted = 0";
            if (cmd.Connection?.State != System.Data.ConnectionState.Open)
                await cmd.Connection!.OpenAsync(ct);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var sId = reader.GetGuid(0);
                var fn = reader.GetString(1);
                var ln = reader.GetString(2);
                var gl = reader.IsDBNull(3) ? 12 : reader.GetInt32(3);
                studentDetails[sId] = ($"{fn} {ln}", $"{gl}. Sınıf");
            }
        }
        else
        {
            foreach (var sid in studentIds)
            {
                studentDetails[sid] = ("Öğrenci", "12. Sınıf");
            }
        }

        var result = new List<CoachMeetingDto>();
        var now = DateTimeOffset.UtcNow;

        foreach (var m in meetings)
        {
            var (name, grade) = studentDetails.TryGetValue(m.StudentId, out var det) ? det : ("Öğrenci", "12. Sınıf");
            var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p[0])).ToUpper();
            if (string.IsNullOrEmpty(initials)) initials = "ÖG";

            string statusStr = m.Status switch
            {
                CoachMeetingStatus.Completed => "completed",
                CoachMeetingStatus.Cancelled => "cancelled",
                _ => m.EndTime < now ? "completed" : "upcoming"
            };

            var localStart = m.StartTime.ToLocalTime();
            var dateStr = localStart.Date == DateTime.Today
                ? "Bugün"
                : (localStart.Date == DateTime.Today.AddDays(1)
                    ? "Yarın"
                    : localStart.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR")));

            var timeStr = $"{localStart:HH:mm} - {m.EndTime.ToLocalTime():HH:mm}";

            result.Add(new CoachMeetingDto(
                m.Id,
                m.StudentId,
                name,
                initials,
                grade,
                "Sayısal",
                dateStr,
                timeStr,
                m.DurationMinutes,
                m.MeetingType,
                m.LocationType,
                statusStr,
                m.Notes,
                m.StartTime,
                m.EndTime
            ));
        }

        return result;
    }

    public async Task<CoachMeetingDto> CreateAsync(CreateCoachMeetingRequest req, CancellationToken ct = default)
    {
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);
        var coachId = coach?.Id ?? Guid.Empty;
        if (coachId == Guid.Empty)
            throw new InvalidOperationException("Toplantı oluşturmak için koç profili gereklidir.");

        if (req.DurationMinutes <= 0 || req.DurationMinutes > 480)
            throw new ArgumentException("Görüşme süresi 1 ile 480 dakika arasında olmalıdır.");

        if (req.StartTime < DateTimeOffset.UtcNow.AddMinutes(-30))
            throw new ArgumentException("Geçmiş bir zamana görüşme planlanamaz.");

        await EnsureAssignmentAsync(coachId, req.StudentId, institutionId, ct);

        var endTime = req.StartTime.AddMinutes(req.DurationMinutes);

        // Check overlapping active meetings for the coach
        var overlap = await db.Set<CoachMeeting>().AsNoTracking()
            .AnyAsync(m => m.InstitutionId == institutionId
                        && m.CoachId == coachId
                        && !m.IsDeleted
                        && m.Status == CoachMeetingStatus.Upcoming
                        && m.StartTime < endTime
                        && m.EndTime > req.StartTime, ct);

        if (overlap)
            throw new ConflictException("Bu saat aralığında koçun başka bir aktif görüşmesi bulunmaktadır.");

        var meeting = new CoachMeeting
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            CoachId = coachId,
            StudentId = req.StudentId,
            StartTime = req.StartTime,
            EndTime = endTime,
            DurationMinutes = req.DurationMinutes,
            MeetingType = string.IsNullOrWhiteSpace(req.MeetingType) ? "Haftalık Takip & Analiz" : req.MeetingType.Trim(),
            LocationType = string.IsNullOrWhiteSpace(req.LocationType) ? "Online Video" : req.LocationType.Trim(),
            Status = CoachMeetingStatus.Upcoming,
            Notes = req.Notes?.Trim()
        };

        db.Set<CoachMeeting>().Add(meeting);
        await db.SaveChangesAsync(ct);

        if (notificationService != null)
        {
            try
            {
                var student = await db.Set<Student>().AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == req.StudentId && s.InstitutionId == institutionId, ct);
                if (student?.UserId != null)
                {
                    await notificationService.CreateNotificationAsync(
                        institutionId,
                        student.UserId.Value,
                        NotificationType.CoachMeetingScheduled,
                        "Yeni Koçluk Görüşmesi",
                        $"{meeting.MeetingType} görüşmesi planlandı ({req.StartTime:dd.MM.yyyy HH:mm}).",
                        "CoachMeeting",
                        meeting.Id,
                        ct);
                }
            }
            catch { }
        }

        await audit.LogAsync(
            currentUser.UserId ?? Guid.Empty,
            "COACH_MEETING_CREATED",
            "CoachMeeting",
            meeting.Id.ToString(),
            institutionId,
            $"Coach {coachId} scheduled meeting with student {req.StudentId} at {req.StartTime}",
            cancellationToken: ct);

        return (await ListAsync(ct)).First(m => m.Id == meeting.Id);
    }

    public async Task<CoachMeetingDto> UpdateAsync(Guid meetingId, UpdateCoachMeetingRequest req, CancellationToken ct = default)
    {
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);

        var meeting = await db.Set<CoachMeeting>()
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.InstitutionId == institutionId && !m.IsDeleted, ct)
            ?? throw new NotFoundException("Görüşme bulunamadı.");

        if (coach != null && meeting.CoachId != coach.Id)
            throw new ForbiddenException("Yalnızca kendi görüşmelerinizi güncelleyebilirsiniz.");

        if (req.Status != null)
        {
            meeting.Status = req.Status.ToLowerInvariant() switch
            {
                "completed" => CoachMeetingStatus.Completed,
                "cancelled" => CoachMeetingStatus.Cancelled,
                _ => CoachMeetingStatus.Upcoming
            };
        }

        if (req.Notes != null)
            meeting.Notes = req.Notes.Trim();

        if (req.MeetingType != null)
            meeting.MeetingType = req.MeetingType.Trim();

        if (req.LocationType != null)
            meeting.LocationType = req.LocationType.Trim();

        if (req.StartTime.HasValue || req.DurationMinutes.HasValue)
        {
            var newStart = req.StartTime ?? meeting.StartTime;
            var newDuration = req.DurationMinutes ?? meeting.DurationMinutes;
            var newEnd = newStart.AddMinutes(newDuration);

            // Check overlap if time changed
            var overlap = await db.Set<CoachMeeting>().AsNoTracking()
                .AnyAsync(m => m.InstitutionId == institutionId
                            && m.CoachId == meeting.CoachId
                            && m.Id != meeting.Id
                            && !m.IsDeleted
                            && m.Status == CoachMeetingStatus.Upcoming
                            && m.StartTime < newEnd
                            && m.EndTime > newStart, ct);

            if (overlap)
                throw new ConflictException("Bu saat aralığında koçun çakışan başka bir görüşmesi bulunmaktadır.");

            meeting.StartTime = newStart;
            meeting.DurationMinutes = newDuration;
            meeting.EndTime = newEnd;
        }

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(
            currentUser.UserId ?? Guid.Empty,
            "COACH_MEETING_UPDATED",
            "CoachMeeting",
            meeting.Id.ToString(),
            institutionId,
            $"Coach meeting {meetingId} updated to status {meeting.Status}",
            cancellationToken: ct);

        return (await ListAsync(ct)).First(m => m.Id == meeting.Id);
    }

    public async Task DeleteAsync(Guid meetingId, CancellationToken ct = default)
    {
        var (institutionId, coach) = await ResolveCoachContextAsync(ct);

        var meeting = await db.Set<CoachMeeting>()
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.InstitutionId == institutionId && !m.IsDeleted, ct)
            ?? throw new NotFoundException("Görüşme bulunamadı.");

        if (coach != null && meeting.CoachId != coach.Id)
            throw new ForbiddenException("Yalnızca kendi görüşmelerinizi silebilirsiniz.");

        meeting.IsDeleted = true;
        meeting.DeletedAt = DateTimeOffset.UtcNow;
        meeting.DeletedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(
            currentUser.UserId ?? Guid.Empty,
            "COACH_MEETING_DELETED",
            "CoachMeeting",
            meeting.Id.ToString(),
            institutionId,
            $"Coach meeting {meetingId} soft-deleted",
            cancellationToken: ct);
    }
}

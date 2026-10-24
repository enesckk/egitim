using System.Security;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Features.ManageMessages;

public sealed class MessagesHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAuditService auditService,
    INotificationService? notificationService = null)
{
    private static readonly HashSet<string> AllowedRoles =
    [
        Roles.SuperAdmin,
        Roles.InstitutionAdmin,
        Roles.Coach,
        Roles.Teacher,
        Roles.Student,
        Roles.Parent
    ];

    public async Task<List<ConversationSummaryDto>> ListConversationsAsync(CancellationToken ct = default)
    {
        var currentUserId = currentUser.UserId ?? throw new UnauthorizedAccessException("Giriş yapılmış kullanıcı gereklidir.");
        var institutionId = await currentUser.GetInstitutionIdAsync() ?? throw new UnauthorizedAccessException("Kurum bağlamı gereklidir.");

        var conversations = await db.Set<Conversation>().AsNoTracking()
            .Where(c => c.InstitutionId == institutionId
                     && !c.IsDeleted
                     && (c.Participant1Id == currentUserId || c.Participant2Id == currentUserId))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

        if (conversations.Count == 0) return [];

        var otherUserIds = conversations
            .Select(c => c.Participant1Id == currentUserId ? c.Participant2Id : c.Participant1Id)
            .Distinct()
            .ToList();

        var usersInfo = await (
            from u in db.Set<ApplicationUser>().AsNoTracking()
            where otherUserIds.Contains(u.Id) && u.InstitutionId == institutionId && !u.IsDeleted
            join ur in db.Set<IdentityUserRole<Guid>>().AsNoTracking() on u.Id equals ur.UserId into userRoles
            from ur in userRoles.DefaultIfEmpty()
            join r in db.Set<ApplicationRole>().AsNoTracking() on ur.RoleId equals r.Id into roles
            from r in roles.DefaultIfEmpty()
            select new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                RoleName = r != null ? r.Name : "User"
            }
        ).ToListAsync(ct);

        var userInfoMap = usersInfo
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var unreadCounts = await db.Set<Message>().AsNoTracking()
            .Where(m => m.InstitutionId == institutionId
                     && m.RecipientUserId == currentUserId
                     && !m.IsRead
                     && !m.IsDeleted)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count, ct);

        var coaches = await db.Set<Coach>().AsNoTracking()
            .Where(c => otherUserIds.Contains(c.UserId) && c.InstitutionId == institutionId && !c.IsDeleted)
            .ToDictionaryAsync(c => c.UserId, c => $"{c.FirstName} {c.LastName}".Trim(), ct);

        var students = await db.Set<Student>().AsNoTracking()
            .Where(s => s.UserId.HasValue && otherUserIds.Contains(s.UserId.Value) && s.InstitutionId == institutionId && !s.IsDeleted)
            .ToDictionaryAsync(s => s.UserId!.Value, s => $"{s.FirstName} {s.LastName}".Trim(), ct);

        var teachers = await db.Set<Teacher>().AsNoTracking()
            .Where(t => otherUserIds.Contains(t.UserId) && t.InstitutionId == institutionId && !t.IsDeleted)
            .ToDictionaryAsync(t => t.UserId, t => $"{t.FirstName} {t.LastName}".Trim(), ct);

        var parents = await db.Set<Parent>().AsNoTracking()
            .Where(p => p.UserId.HasValue && otherUserIds.Contains(p.UserId.Value) && p.InstitutionId == institutionId && !p.IsDeleted)
            .ToDictionaryAsync(p => p.UserId!.Value, p => $"{p.FirstName} {p.LastName}".Trim(), ct);

        var results = new List<ConversationSummaryDto>();
        foreach (var conv in conversations)
        {
            var otherId = conv.Participant1Id == currentUserId ? conv.Participant2Id : conv.Participant1Id;
            userInfoMap.TryGetValue(otherId, out var otherUser);

            var name = "";
            if (coaches.TryGetValue(otherId, out var coachName) && !string.IsNullOrWhiteSpace(coachName)) name = coachName;
            else if (students.TryGetValue(otherId, out var studentName) && !string.IsNullOrWhiteSpace(studentName)) name = studentName;
            else if (teachers.TryGetValue(otherId, out var teacherName) && !string.IsNullOrWhiteSpace(teacherName)) name = teacherName;
            else if (parents.TryGetValue(otherId, out var parentName) && !string.IsNullOrWhiteSpace(parentName)) name = parentName;
            else if (otherUser != null && !string.IsNullOrWhiteSpace($"{otherUser.FirstName} {otherUser.LastName}".Trim())) name = $"{otherUser.FirstName} {otherUser.LastName}".Trim();
            else name = otherUser?.RoleName != null ? $"{otherUser.RoleName} Kullanıcısı" : "Kullanıcı";

            var role = otherUser?.RoleName ?? "User";
            var unread = unreadCounts.TryGetValue(conv.Id, out var count) ? count : 0;

            results.Add(new ConversationSummaryDto(
                conv.Id,
                otherId,
                name,
                role,
                conv.Subject,
                conv.LastMessageSnippet,
                conv.LastMessageAt,
                unread
            ));
        }

        return results;
    }

    public async Task<List<MessageDto>> GetConversationMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var currentUserId = currentUser.UserId ?? throw new UnauthorizedAccessException("Giriş yapılmış kullanıcı gereklidir.");
        var institutionId = await currentUser.GetInstitutionIdAsync() ?? throw new UnauthorizedAccessException("Kurum bağlamı gereklidir.");

        var conv = await db.Set<Conversation>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.InstitutionId == institutionId && !c.IsDeleted, ct)
            ?? throw new NotFoundException($"Konuşma bulunamadı: {conversationId}");

        if (conv.Participant1Id != currentUserId && conv.Participant2Id != currentUserId && !currentUser.IsSuperAdmin)
        {
            throw new SecurityException("Bu konuşmaya erişim yetkiniz bulunmamaktadır.");
        }

        var messages = await db.Set<Message>().AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.InstitutionId == institutionId && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return [];

        var senderIds = messages.Select(m => m.SenderUserId).Distinct().ToList();
        var senders = await (
            from u in db.Set<ApplicationUser>().AsNoTracking()
            where senderIds.Contains(u.Id) && !u.IsDeleted
            join ur in db.Set<IdentityUserRole<Guid>>().AsNoTracking() on u.Id equals ur.UserId into userRoles
            from ur in userRoles.DefaultIfEmpty()
            join r in db.Set<ApplicationRole>().AsNoTracking() on ur.RoleId equals r.Id into roles
            from r in roles.DefaultIfEmpty()
            select new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                RoleName = r != null ? r.Name : "User"
            }
        ).ToListAsync(ct);

        var senderMap = senders.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());

        return messages.Select(m =>
        {
            senderMap.TryGetValue(m.SenderUserId, out var sender);
            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}".Trim() : "Kullanıcı";
            var senderRole = sender?.RoleName ?? "User";

            return new MessageDto(
                m.Id,
                m.ConversationId,
                m.SenderUserId,
                senderName,
                senderRole,
                m.Content,
                m.CreatedAt,
                m.IsRead,
                m.ReadAt
            );
        }).ToList();
    }

    public async Task<int> GetTotalUnreadCountAsync(CancellationToken ct = default)
    {
        var currentUserId = currentUser.UserId ?? throw new UnauthorizedAccessException("Giriş yapılmış kullanıcı gereklidir.");
        var institutionId = await currentUser.GetInstitutionIdAsync() ?? throw new UnauthorizedAccessException("Kurum bağlamı gereklidir.");

        return await db.Set<Message>().AsNoTracking()
            .CountAsync(m => m.InstitutionId == institutionId
                          && m.RecipientUserId == currentUserId
                          && !m.IsRead
                          && !m.IsDeleted, ct);
    }

    public async Task MarkConversationAsReadAsync(Guid conversationId, CancellationToken ct = default)
    {
        var currentUserId = currentUser.UserId ?? throw new UnauthorizedAccessException("Giriş yapılmış kullanıcı gereklidir.");
        var institutionId = await currentUser.GetInstitutionIdAsync() ?? throw new UnauthorizedAccessException("Kurum bağlamı gereklidir.");

        var conv = await db.Set<Conversation>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.InstitutionId == institutionId && !c.IsDeleted, ct)
            ?? throw new NotFoundException($"Konuşma bulunamadı: {conversationId}");

        if (conv.Participant1Id != currentUserId && conv.Participant2Id != currentUserId && !currentUser.IsSuperAdmin)
        {
            throw new SecurityException("Bu konuşmaya erişim yetkiniz bulunmamaktadır.");
        }

        var unreadMessages = await db.Set<Message>()
            .Where(m => m.ConversationId == conversationId
                     && m.InstitutionId == institutionId
                     && m.RecipientUserId == currentUserId
                     && !m.IsRead
                     && !m.IsDeleted)
            .ToListAsync(ct);

        if (unreadMessages.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var m in unreadMessages)
            {
                m.IsRead = true;
                m.ReadAt = now;
            }
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageRequest req, CancellationToken ct = default)
    {
        var currentUserId = currentUser.UserId ?? throw new UnauthorizedAccessException("Giriş yapılmış kullanıcı gereklidir.");
        var institutionId = await currentUser.GetInstitutionIdAsync() ?? throw new UnauthorizedAccessException("Kurum bağlamı gereklidir.");

        if (string.IsNullOrWhiteSpace(req.Content))
            throw new ArgumentException("Mesaj içeriği boş olamaz.");

        var content = req.Content.Trim();
        if (content.Length > 4000)
            throw new ArgumentException("Mesaj en fazla 4000 karakter olabilir.");

        Conversation? conv = null;
        Guid recipientUserId = Guid.Empty;

        if (req.ConversationId.HasValue)
        {
            conv = await db.Set<Conversation>()
                .FirstOrDefaultAsync(c => c.Id == req.ConversationId.Value && c.InstitutionId == institutionId && !c.IsDeleted, ct)
                ?? throw new NotFoundException($"Konuşma bulunamadı: {req.ConversationId.Value}");

            if (conv.Participant1Id != currentUserId && conv.Participant2Id != currentUserId && !currentUser.IsSuperAdmin)
                throw new SecurityException("Bu konuşmaya mesaj gönderme yetkiniz bulunmamaktadır.");

            recipientUserId = conv.Participant1Id == currentUserId ? conv.Participant2Id : conv.Participant1Id;
        }
        else if (req.RecipientUserId.HasValue)
        {
            recipientUserId = req.RecipientUserId.Value;
            if (recipientUserId == currentUserId)
                throw new ArgumentException("Kendinize mesaj gönderemezsiniz.");

            var (p1, p2) = OrderParticipants(currentUserId, recipientUserId);
            conv = await db.Set<Conversation>()
                .FirstOrDefaultAsync(c => c.InstitutionId == institutionId
                                       && c.Participant1Id == p1
                                       && c.Participant2Id == p2
                                       && !c.IsDeleted, ct);
        }
        else
        {
            throw new ArgumentException("Konuşma ID veya Alıcı Kullanıcı ID belirtilmelidir.");
        }

        // Validate relationship strictly
        await ValidateCommunicationRelationshipAsync(currentUserId, recipientUserId, institutionId, ct);

        var now = DateTimeOffset.UtcNow;
        if (conv == null)
        {
            var (p1, p2) = OrderParticipants(currentUserId, recipientUserId);
            conv = new Conversation
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                Participant1Id = p1,
                Participant2Id = p2,
                Subject = req.Subject?.Trim(),
                LastMessageAt = now,
                LastMessageSnippet = content.Length > 100 ? content[..97] + "..." : content,
                CreatedAt = now,
                CreatedBy = currentUserId
            };
            db.Set<Conversation>().Add(conv);
        }
        else
        {
            conv.LastMessageAt = now;
            conv.LastMessageSnippet = content.Length > 100 ? content[..97] + "..." : content;
            conv.UpdatedAt = now;
            conv.UpdatedBy = currentUserId;
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            ConversationId = conv.Id,
            SenderUserId = currentUserId,
            RecipientUserId = recipientUserId,
            Content = content,
            IsRead = false,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        db.Set<Message>().Add(message);
        await db.SaveChangesAsync(ct);

        // Notify recipient about new message
        if (notificationService != null)
        {
            try
            {
                await notificationService.CreateNotificationAsync(
                    institutionId,
                    recipientUserId,
                    NotificationType.NewMessageReceived,
                    "Yeni Mesajınız Var",
                    $"{content[..Math.Min(content.Length, 60)]}...",
                    "Message",
                    message.Id,
                    ct);
            }
            catch { }
        }

        await auditService.LogAsync(
            userId: currentUserId,
            action: "MessageSent",
            entityType: "Message",
            entityId: message.Id.ToString(),
            institutionId: institutionId,
            metadataJson: $"{{\"conversationId\":\"{conv.Id}\",\"recipientUserId\":\"{recipientUserId}\"}}",
            cancellationToken: ct);

        // Fetch sender info for response
        var senderUser = await db.Set<ApplicationUser>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        var senderName = senderUser != null ? $"{senderUser.FirstName} {senderUser.LastName}".Trim() : "Kullanıcı";
        var senderRole = currentUser.Roles.FirstOrDefault() ?? "User";

        return new MessageDto(
            message.Id,
            conv.Id,
            currentUserId,
            senderName,
            senderRole,
            message.Content,
            message.CreatedAt,
            false,
            null
        );
    }

    private static (Guid p1, Guid p2) OrderParticipants(Guid u1, Guid u2)
    {
        return u1.CompareTo(u2) < 0 ? (u1, u2) : (u2, u1);
    }

    public async Task ValidateCommunicationRelationshipAsync(
        Guid senderUserId, Guid recipientUserId, Guid institutionId, CancellationToken ct)
    {
        // 1. Both users must be active and belonging to the same institution
        var recipient = await (
            from u in db.Set<ApplicationUser>().AsNoTracking()
            where u.Id == recipientUserId && u.InstitutionId == institutionId && u.IsActive && !u.IsDeleted
            join ur in db.Set<IdentityUserRole<Guid>>().AsNoTracking() on u.Id equals ur.UserId into userRoles
            from ur in userRoles.DefaultIfEmpty()
            join r in db.Set<ApplicationRole>().AsNoTracking() on ur.RoleId equals r.Id into roles
            from r in roles.DefaultIfEmpty()
            select new { u.Id, RoleName = r != null ? r.Name : null }
        ).FirstOrDefaultAsync(ct);

        if (recipient == null)
            throw new SecurityException("Alıcı kullanıcı bulunamadı veya bu kurumda aktif değil.");

        // SuperAdmin can communicate with anyone
        if (currentUser.IsSuperAdmin) return;

        var senderRoles = currentUser.Roles;
        var recipientRole = recipient.RoleName ?? string.Empty;

        // If either party has InstitutionAdmin role in this institution, allowed
        if (senderRoles.Contains(Roles.InstitutionAdmin) || recipientRole == Roles.InstitutionAdmin)
            return;

        // Check relationship matrix
        // A) Coach <-> Student
        if (senderRoles.Contains(Roles.Coach) && recipientRole == Roles.Student)
        {
            if (await IsCoachAssignedToStudentUserAsync(senderUserId, recipientUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca size aktif olarak atanmış öğrencilerle mesajlaşabilirsiniz.");
        }
        if (senderRoles.Contains(Roles.Student) && recipientRole == Roles.Coach)
        {
            if (await IsCoachAssignedToStudentUserAsync(recipientUserId, senderUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca kurumunuz tarafından size atanan danışman koçunuzla mesajlaşabilirsiniz.");
        }

        // B) Teacher <-> Student
        if (senderRoles.Contains(Roles.Teacher) && recipientRole == Roles.Student)
        {
            if (await IsTeacherEnrolledWithStudentUserAsync(senderUserId, recipientUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca dersine girdiğiniz sınıflardaki öğrencilerle mesajlaşabilirsiniz.");
        }
        if (senderRoles.Contains(Roles.Student) && recipientRole == Roles.Teacher)
        {
            if (await IsTeacherEnrolledWithStudentUserAsync(recipientUserId, senderUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca dersinize giren öğretmenlerinizle mesajlaşabilirsiniz.");
        }

        // C) Parent <-> Coach
        if (senderRoles.Contains(Roles.Parent) && recipientRole == Roles.Coach)
        {
            if (await IsParentRelatedToCoachAsync(senderUserId, recipientUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca öğrencinize atanmış danışman koçla mesajlaşabilirsiniz.");
        }
        if (senderRoles.Contains(Roles.Coach) && recipientRole == Roles.Parent)
        {
            if (await IsParentRelatedToCoachAsync(recipientUserId, senderUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca aktif danışmanlık verdiğiniz öğrencilerin velileriyle mesajlaşabilirsiniz.");
        }

        // D) Parent <-> Teacher
        if (senderRoles.Contains(Roles.Parent) && recipientRole == Roles.Teacher)
        {
            if (await IsParentRelatedToTeacherAsync(senderUserId, recipientUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca öğrencinizin dersine giren öğretmenlerle mesajlaşabilirsiniz.");
        }
        if (senderRoles.Contains(Roles.Teacher) && recipientRole == Roles.Parent)
        {
            if (await IsParentRelatedToTeacherAsync(recipientUserId, senderUserId, institutionId, ct))
                return;
            throw new SecurityException("Yalnızca sınıflarınızdaki öğrencilerin velileriyle mesajlaşabilirsiniz.");
        }

        // E) Teacher <-> Coach (within institution)
        if ((senderRoles.Contains(Roles.Teacher) && recipientRole == Roles.Coach) ||
            (senderRoles.Contains(Roles.Coach) && recipientRole == Roles.Teacher))
        {
            return;
        }

        // All other permutations fail closed
        throw new SecurityException("Bu kullanıcı ile mesajlaşma yetkiniz bulunmamaktadır.");
    }

    private async Task<bool> IsCoachAssignedToStudentUserAsync(
        Guid coachUserId, Guid studentUserId, Guid institutionId, CancellationToken ct)
    {
        var coach = await db.Set<Coach>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == coachUserId && c.InstitutionId == institutionId && !c.IsDeleted, ct);
        if (coach == null) return false;

        var student = await db.Set<Student>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == studentUserId && s.InstitutionId == institutionId && !s.IsDeleted, ct);
        if (student == null) return false;

        return await db.Set<StudentCoachAssignment>().AsNoTracking()
            .AnyAsync(a => a.InstitutionId == institutionId
                        && a.CoachId == coach.Id
                        && a.StudentId == student.Id
                        && a.IsActive
                        && a.EndedAt == null, ct);
    }

    private async Task<bool> IsTeacherEnrolledWithStudentUserAsync(
        Guid teacherUserId, Guid studentUserId, Guid institutionId, CancellationToken ct)
    {
        var teacher = await db.Set<Teacher>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == teacherUserId && t.InstitutionId == institutionId && !t.IsDeleted, ct);
        if (teacher == null) return false;

        var student = await db.Set<Student>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == studentUserId && s.InstitutionId == institutionId && !s.IsDeleted, ct);
        if (student == null) return false;

        return await (
            from ctLink in db.Set<ClassroomTeacher>().AsNoTracking()
            join c in db.Set<Classroom>().AsNoTracking() on ctLink.ClassroomId equals c.Id
            join cs in db.Set<ClassroomStudent>().AsNoTracking() on c.Id equals cs.ClassroomId
            where ctLink.TeacherId == teacher.Id
               && ctLink.InstitutionId == institutionId
               && !ctLink.IsDeleted
               && c.InstitutionId == institutionId
               && !c.IsDeleted
               && cs.StudentId == student.Id
               && cs.InstitutionId == institutionId
               && !cs.IsDeleted
            select cs.StudentId
        ).AnyAsync(ct);
    }

    private async Task<bool> IsParentRelatedToCoachAsync(
        Guid parentUserId, Guid coachUserId, Guid institutionId, CancellationToken ct)
    {
        var parent = await db.Set<Parent>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == parentUserId && p.InstitutionId == institutionId && !p.IsDeleted, ct);
        if (parent == null) return false;

        var coach = await db.Set<Coach>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == coachUserId && c.InstitutionId == institutionId && !c.IsDeleted, ct);
        if (coach == null) return false;

        var studentIds = await db.Set<StudentParent>().AsNoTracking()
            .Where(sp => sp.ParentId == parent.Id && sp.InstitutionId == institutionId && !sp.IsDeleted)
            .Select(sp => sp.StudentId)
            .ToListAsync(ct);

        if (studentIds.Count == 0) return false;

        return await db.Set<StudentCoachAssignment>().AsNoTracking()
            .AnyAsync(a => a.InstitutionId == institutionId
                        && a.CoachId == coach.Id
                        && studentIds.Contains(a.StudentId)
                        && a.IsActive
                        && a.EndedAt == null, ct);
    }

    private async Task<bool> IsParentRelatedToTeacherAsync(
        Guid parentUserId, Guid teacherUserId, Guid institutionId, CancellationToken ct)
    {
        var parent = await db.Set<Parent>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == parentUserId && p.InstitutionId == institutionId && !p.IsDeleted, ct);
        if (parent == null) return false;

        var teacher = await db.Set<Teacher>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == teacherUserId && t.InstitutionId == institutionId && !t.IsDeleted, ct);
        if (teacher == null) return false;

        var studentIds = await db.Set<StudentParent>().AsNoTracking()
            .Where(sp => sp.ParentId == parent.Id && sp.InstitutionId == institutionId && !sp.IsDeleted)
            .Select(sp => sp.StudentId)
            .ToListAsync(ct);

        if (studentIds.Count == 0) return false;

        return await (
            from ctLink in db.Set<ClassroomTeacher>().AsNoTracking()
            join c in db.Set<Classroom>().AsNoTracking() on ctLink.ClassroomId equals c.Id
            join cs in db.Set<ClassroomStudent>().AsNoTracking() on c.Id equals cs.ClassroomId
            where ctLink.TeacherId == teacher.Id
               && ctLink.InstitutionId == institutionId
               && !ctLink.IsDeleted
               && c.InstitutionId == institutionId
               && !c.IsDeleted
               && studentIds.Contains(cs.StudentId)
               && cs.InstitutionId == institutionId
               && !cs.IsDeleted
            select cs.StudentId
        ).AnyAsync(ct);
    }
}

using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Teachers.Features.GetTeacherDashboard;

public class GetTeacherDashboardHandler(
    IApplicationDbContext db,
    ICurrentUser user,
    IAcademicCatalog academic)
{
    private sealed record RawAttemptItem(
        Guid StudentId,
        DateTimeOffset TakenAt,
        string ExamTitle,
        string SubjectName,
        string TopicName,
        int Wrong,
        int Correct,
        decimal Net
    );

    public async Task<TeacherDashboardDto> HandleAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            throw new ForbiddenException("Access denied.");

        var institutionId = await user.GetInstitutionIdAsync();
        if (!institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var teacher = await db.Set<Teacher>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == user.UserId.Value && t.InstitutionId == institutionId.Value && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Teacher profile not found for user.");

        var teacherSubjects = await db.Set<TeacherSubject>().AsNoTracking()
            .Where(ts => ts.TeacherId == teacher.Id && ts.InstitutionId == teacher.InstitutionId && !ts.IsDeleted)
            .ToListAsync(ct);

        var subjectIds = teacherSubjects.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNamesMap = await academic.GetSubjectNamesAsync(subjectIds, ct);
        var assignedSubjectNames = subjectIds
            .Select(sid => subjectNamesMap.GetValueOrDefault(sid, "Bilinmeyen Ders"))
            .Distinct()
            .ToList();

        var classroomIds = await db.Set<ClassroomTeacher>().AsNoTracking()
            .Where(ctLink => ctLink.TeacherId == teacher.Id && ctLink.InstitutionId == teacher.InstitutionId && !ctLink.IsDeleted)
            .Select(ctLink => ctLink.ClassroomId)
            .Distinct()
            .ToListAsync(ct);

        // Fetch student IDs belonging to the teacher's classrooms
        var studentIds = await db.Set<ClassroomStudent>().AsNoTracking()
            .Where(cs => classroomIds.Contains(cs.ClassroomId) && cs.InstitutionId == teacher.InstitutionId && !cs.IsDeleted)
            .Select(cs => cs.StudentId)
            .Distinct()
            .ToListAsync(ct);

        if (studentIds.Count == 0)
        {
            return new TeacherDashboardDto(
                $"{teacher.FirstName} {teacher.LastName}",
                assignedSubjectNames,
                0,
                classroomIds.Count,
                [],
                []
            );
        }

        // Direct SQL query using EF Core Database for raw attempt results to avoid circular entity dependencies
        var studentIdsFormatted = string.Join(",", studentIds.Select(id => $"'{id}'"));
        var sql = $@"
            SELECT 
                a.StudentId,
                a.TakenAt,
                e.Title AS ExamTitle,
                sub.Name AS SubjectName,
                t.Name AS TopicName,
                tr.Wrong,
                tr.Correct,
                tr.Net
            FROM StudentExamAttempts a
            JOIN Exams e ON a.ExamId = e.Id AND e.IsDeleted = 0
            JOIN StudentExamTopicResults tr ON tr.StudentExamAttemptId = a.Id AND tr.IsDeleted = 0
            JOIN AcademicTopics t ON tr.TopicId = t.Id
            JOIN AcademicSubjects sub ON t.SubjectId = sub.Id
            WHERE a.StudentId IN ({studentIdsFormatted})
              AND a.InstitutionId = '{institutionId.Value}'
              AND a.IsDeleted = 0
              AND a.Status = 1
            ORDER BY a.TakenAt DESC";

        // Query students info
        var studentInfos = await (from s in db.Set<ClassroomStudent>().AsNoTracking()
                                  where studentIds.Contains(s.StudentId)
                                  select s)
                                  .ToListAsync(ct);

        // Query student names directly from Students table via SQL query
        var studentDetailsSql = $@"
            SELECT Id, FirstName, LastName, GradeLevel
            FROM Students
            WHERE Id IN ({studentIdsFormatted}) AND InstitutionId = '{institutionId.Value}' AND IsDeleted = 0";

        var students = new List<(Guid Id, string FirstName, string LastName, int? GradeLevel)>();
        var rawAttempts = new List<RawAttemptItem>();

        // Execute via DbContext connection if relational
        if (db is Microsoft.EntityFrameworkCore.DbContext efDb && efDb.Database.IsRelational())
        {
            using var command = efDb.Database.GetDbConnection().CreateCommand();
            command.CommandText = studentDetailsSql;
            if (command.Connection?.State != System.Data.ConnectionState.Open)
            {
                await command.Connection!.OpenAsync(ct);
            }

            using (var reader = await command.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    var id = reader.GetGuid(0);
                    var fn = reader.GetString(1);
                    var ln = reader.GetString(2);
                    int? gl = reader.IsDBNull(3) ? null : reader.GetInt32(3);
                    students.Add((id, fn, ln, gl));
                }
            }

            using var attemptCmd = efDb.Database.GetDbConnection().CreateCommand();
            attemptCmd.CommandText = sql;
            using (var reader = await attemptCmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    var stId = reader.GetGuid(0);
                    var takenAt = reader.GetFieldValue<DateTimeOffset>(1);
                    var title = reader.GetString(2);
                    var subName = reader.GetString(3);
                    var topName = reader.GetString(4);
                    var wrong = reader.GetInt32(5);
                    var correct = reader.GetInt32(6);
                    var net = reader.GetDecimal(7);
                    rawAttempts.Add(new RawAttemptItem(stId, takenAt, title, subName, topName, wrong, correct, net));
                }
            }
        }
        else
        {
            // In-Memory or non-relational fallback
            foreach (var sid in studentIds)
            {
                students.Add((sid, "Öğrenci", sid.ToString()[..4], 11));
            }
        }

        var studentMap = students.ToDictionary(s => s.Id);

        // Calculate Academic Gaps (Topics with wrong answers, sorted by wrong count)
        var gapGroups = rawAttempts
            .Where(r => r.Wrong > 0)
            .GroupBy(r => new { r.StudentId, r.SubjectName, r.TopicName })
            .Select(g => new
            {
                g.Key.StudentId,
                g.Key.SubjectName,
                g.Key.TopicName,
                TotalWrong = g.Sum(x => x.Wrong),
                TotalCorrect = g.Sum(x => x.Correct),
                LastAttempt = g.OrderByDescending(x => x.TakenAt).First()
            })
            .OrderByDescending(g => g.TotalWrong)
            .Take(8)
            .ToList();

        var academicGaps = new List<TeacherAcademicGapDto>();
        int gapCounter = 1;
        foreach (var g in gapGroups)
        {
            var st = studentMap.GetValueOrDefault(g.StudentId);
            var fullName = st != default ? $"{st.FirstName} {st.LastName}" : "Öğrenci";
            var initials = st != default && st.FirstName.Length > 0 && st.LastName.Length > 0
                ? $"{st.FirstName[0]}{st.LastName[0]}"
                : "ÖG";

            string severity = g.TotalWrong >= 10 ? "critical" : g.TotalWrong >= 5 ? "warning" : "info";
            string detail = g.TotalWrong >= 10
                ? $"Denemelerde toplam {g.TotalWrong} yanlış — kavramda eksiklik tespit edildi"
                : $"Toplam {g.TotalWrong} yanlış — soru çözümü ve pekiştirme önerilir";

            academicGaps.Add(new TeacherAcademicGapDto(
                $"gap-{gapCounter++}",
                g.StudentId.ToString(),
                fullName,
                st.GradeLevel.HasValue ? $"{st.GradeLevel}. Sınıf" : "12. Sınıf",
                g.SubjectName,
                g.TopicName,
                detail,
                severity,
                initials,
                g.LastAttempt.TakenAt.ToString("dd MMM yyyy")
            ));
        }

        // Calculate Student Nets & Trend
        var studentNets = new List<TeacherStudentNetDto>();
        int netCounter = 1;
        foreach (var s in students)
        {
            var initials = s.FirstName.Length > 0 && s.LastName.Length > 0
                ? $"{s.FirstName[0]}{s.LastName[0]}"
                : "ÖG";

            var studentAttempts = rawAttempts
                .Where(r => r.StudentId == s.Id)
                .GroupBy(r => new { r.TakenAt, r.SubjectName })
                .Select(g => new
                {
                    g.Key.TakenAt,
                    g.Key.SubjectName,
                    TotalNet = g.Sum(x => x.Net)
                })
                .ToList();

            var mathResults = studentAttempts.Where(x => x.SubjectName.Contains("Matematik")).OrderByDescending(x => x.TakenAt).ToList();
            var physResults = studentAttempts.Where(x => x.SubjectName.Contains("Fizik")).OrderByDescending(x => x.TakenAt).ToList();

            decimal? mathNet = mathResults.Count > 0 ? Math.Round(mathResults[0].TotalNet, 1) : null;
            decimal? physNet = physResults.Count > 0 ? Math.Round(physResults[0].TotalNet, 1) : null;

            string trend = "stable";
            if (mathResults.Count >= 2)
            {
                var diff = mathResults[0].TotalNet - mathResults[1].TotalNet;
                if (diff > 0.5m) trend = "up";
                else if (diff < -0.5m) trend = "down";
            }

            studentNets.Add(new TeacherStudentNetDto(
                $"sn-{netCounter++}",
                s.Id.ToString(),
                $"{s.FirstName} {s.LastName}",
                s.GradeLevel.HasValue ? s.GradeLevel.Value.ToString() : "12",
                mathNet,
                physNet,
                trend,
                initials
            ));
        }

        return new TeacherDashboardDto(
            $"{teacher.FirstName} {teacher.LastName}",
            assignedSubjectNames,
            students.Count,
            classroomIds.Count,
            academicGaps,
            studentNets
        );
    }
}

using System.Data;
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
    public async Task<TeacherDashboardDto> HandleAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            throw new ForbiddenException("Access denied.");

        var institutionId = await user.GetInstitutionIdAsync();
        if (!institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        if (db is DbContext efDb && efDb.Database.IsRelational())
        {
            var conn = efDb.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                DECLARE @teacherId uniqueidentifier;
                SELECT TOP (1) @teacherId = t.Id
                FROM Teachers t
                WHERE t.UserId = @userId AND t.InstitutionId = @instId AND t.IsDeleted = 0;

                SELECT t.Id, t.FirstName, t.LastName
                FROM Teachers t
                WHERE t.Id = @teacherId;

                IF @teacherId IS NOT NULL
                BEGIN
                    SELECT DISTINCT ISNULL(sub.Name, N'Bilinmeyen Ders') AS SubjectName
                    FROM TeacherSubjects ts
                    LEFT JOIN AcademicSubjects sub ON sub.Id = ts.SubjectId AND sub.IsDeleted = 0
                    WHERE ts.TeacherId = @teacherId AND ts.InstitutionId = @instId AND ts.IsDeleted = 0;

                    SELECT COUNT(DISTINCT ct.ClassroomId)
                    FROM ClassroomTeachers ct
                    WHERE ct.TeacherId = @teacherId AND ct.InstitutionId = @instId AND ct.IsDeleted = 0;

                    SELECT s.Id, s.FirstName, s.LastName, s.GradeLevel
                    FROM Students s
                    WHERE s.InstitutionId = @instId
                      AND s.IsDeleted = 0
                      AND EXISTS (
                          SELECT 1
                          FROM ClassroomStudents cs
                          INNER JOIN ClassroomTeachers ct
                              ON ct.ClassroomId = cs.ClassroomId
                             AND ct.InstitutionId = @instId
                             AND ct.IsDeleted = 0
                          WHERE cs.StudentId = s.Id
                            AND cs.InstitutionId = @instId
                            AND cs.IsDeleted = 0
                            AND ct.TeacherId = @teacherId
                      );

                    ;WITH TeacherStudentIds AS (
                        SELECT DISTINCT cs.StudentId
                        FROM ClassroomStudents cs
                        INNER JOIN ClassroomTeachers ct
                            ON ct.ClassroomId = cs.ClassroomId
                           AND ct.InstitutionId = @instId
                           AND ct.IsDeleted = 0
                        WHERE cs.InstitutionId = @instId
                          AND cs.IsDeleted = 0
                          AND ct.TeacherId = @teacherId
                    )
                    SELECT TOP (8)
                        a.StudentId,
                        sub.Name AS SubjectName,
                        t.Name AS TopicName,
                        SUM(tr.Wrong) AS TotalWrong,
                        MAX(a.TakenAt) AS LastTakenAt
                    FROM StudentExamAttempts a
                    INNER JOIN TeacherStudentIds ts ON ts.StudentId = a.StudentId
                    INNER JOIN Exams e ON e.Id = a.ExamId AND e.IsDeleted = 0
                    INNER JOIN StudentExamTopicResults tr ON tr.StudentExamAttemptId = a.Id AND tr.IsDeleted = 0
                    INNER JOIN AcademicTopics t ON t.Id = tr.TopicId
                    INNER JOIN AcademicSubjects sub ON sub.Id = t.SubjectId
                    WHERE a.InstitutionId = @instId
                      AND a.IsDeleted = 0
                      AND a.Status = 1
                      AND tr.Wrong > 0
                    GROUP BY a.StudentId, sub.Name, t.Name
                    ORDER BY SUM(tr.Wrong) DESC, MAX(a.TakenAt) DESC;

                    ;WITH TeacherStudentIds AS (
                        SELECT DISTINCT cs.StudentId
                        FROM ClassroomStudents cs
                        INNER JOIN ClassroomTeachers ct
                            ON ct.ClassroomId = cs.ClassroomId
                           AND ct.InstitutionId = @instId
                           AND ct.IsDeleted = 0
                        WHERE cs.InstitutionId = @instId
                          AND cs.IsDeleted = 0
                          AND ct.TeacherId = @teacherId
                    ),
                    TopicBuckets AS (
                        SELECT
                            t.Id AS TopicId,
                            sub.Name AS SubjectName,
                            CASE
                                WHEN sub.Name LIKE N'%Matematik%' THEN 1
                                WHEN sub.Name LIKE N'%Fizik%' THEN 2
                                ELSE 0
                            END AS SubjectBucket
                        FROM AcademicTopics t
                        INNER JOIN AcademicSubjects sub ON sub.Id = t.SubjectId
                        WHERE sub.Name LIKE N'%Matematik%' OR sub.Name LIKE N'%Fizik%'
                    ),
                    SubjectAttemptTotals AS (
                        SELECT
                            a.StudentId,
                            a.TakenAt,
                            tb.SubjectName,
                            tb.SubjectBucket,
                            SUM(tr.Net) AS TotalNet
                        FROM StudentExamAttempts a
                        INNER JOIN TeacherStudentIds ts ON ts.StudentId = a.StudentId
                        INNER JOIN Exams e ON e.Id = a.ExamId AND e.IsDeleted = 0
                        INNER JOIN StudentExamTopicResults tr ON tr.StudentExamAttemptId = a.Id AND tr.IsDeleted = 0
                        INNER JOIN TopicBuckets tb ON tb.TopicId = tr.TopicId
                        WHERE a.InstitutionId = @instId
                          AND a.IsDeleted = 0
                          AND a.Status = 1
                        GROUP BY a.StudentId, a.TakenAt, tb.SubjectName, tb.SubjectBucket
                    ),
                    RankedSubjectAttempts AS (
                        SELECT
                            StudentId,
                            TotalNet,
                            ROW_NUMBER() OVER (
                                PARTITION BY StudentId, SubjectBucket
                                ORDER BY TakenAt DESC
                            ) AS Rn,
                            SubjectBucket
                        FROM SubjectAttemptTotals
                    )
                    SELECT StudentId, SubjectBucket, Rn, TotalNet
                    FROM RankedSubjectAttempts
                    WHERE (SubjectBucket = 1 AND Rn <= 2)
                       OR (SubjectBucket = 2 AND Rn = 1);
                END";

            var userParam = cmd.CreateParameter();
            userParam.ParameterName = "@userId";
            userParam.DbType = DbType.Guid;
            userParam.Value = user.UserId.Value;
            cmd.Parameters.Add(userParam);

            var instParam = cmd.CreateParameter();
            instParam.ParameterName = "@instId";
            instParam.DbType = DbType.Guid;
            instParam.Value = institutionId.Value;
            cmd.Parameters.Add(instParam);

            using var reader = await cmd.ExecuteReaderAsync(ct);

            // Resultset 1: Teacher profile
            if (!await reader.ReadAsync(ct))
                throw new NotFoundException("Teacher profile not found for user.");

            var teacherFirstName = reader.GetString(1);
            var teacherLastName = reader.GetString(2);

            // Resultset 2: Assigned subject names
            var assignedSubjectNames = new List<string>();
            if (await reader.NextResultAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    assignedSubjectNames.Add(reader.GetString(0));
                }
            }

            // Resultset 3: Assigned classroom count
            int totalAssignedClasses = 0;
            if (await reader.NextResultAsync(ct) && await reader.ReadAsync(ct))
            {
                totalAssignedClasses = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            }

            // Resultset 4: Distinct assigned students
            var students = new List<(Guid Id, string FirstName, string LastName, int? GradeLevel)>();
            if (await reader.NextResultAsync(ct))
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

            if (students.Count == 0)
            {
                return new TeacherDashboardDto(
                    $"{teacherFirstName} {teacherLastName}",
                    assignedSubjectNames,
                    0,
                    totalAssignedClasses,
                    [],
                    []
                );
            }

            var studentMap = students.ToDictionary(s => s.Id);

            // Resultset 5: Top 8 Academic Gaps
            var academicGaps = new List<TeacherAcademicGapDto>();
            if (await reader.NextResultAsync(ct))
            {
                int gapCounter = 1;
                while (await reader.ReadAsync(ct))
                {
                    var stId = reader.GetGuid(0);
                    var subName = reader.GetString(1);
                    var topName = reader.GetString(2);
                    var totalWrong = reader.GetInt32(3);
                    var lastTakenAt = reader.GetFieldValue<DateTimeOffset>(4);

                    var st = studentMap.GetValueOrDefault(stId);
                    var fullName = st != default ? $"{st.FirstName} {st.LastName}" : "Öğrenci";
                    var initials = st != default && st.FirstName.Length > 0 && st.LastName.Length > 0
                        ? $"{st.FirstName[0]}{st.LastName[0]}"
                        : "ÖG";

                    string severity = totalWrong >= 10 ? "critical" : totalWrong >= 5 ? "warning" : "info";
                    string detail = totalWrong >= 10
                        ? $"Denemelerde toplam {totalWrong} yanlış — kavramda eksiklik tespit edildi"
                        : $"Toplam {totalWrong} yanlış — soru çözümü ve pekiştirme önerilir";

                    academicGaps.Add(new TeacherAcademicGapDto(
                        $"gap-{gapCounter++}",
                        stId.ToString(),
                        fullName,
                        st.GradeLevel.HasValue ? $"{st.GradeLevel}. Sınıf" : "12. Sınıf",
                        subName,
                        topName,
                        detail,
                        severity,
                        initials,
                        lastTakenAt.ToString("dd MMM yyyy")
                    ));
                }
            }

            // Resultset 6: Latest 2 Math & Latest 1 Physics attempt net totals per student
            var mathLatestByStudent = new Dictionary<Guid, decimal>();
            var mathPrevByStudent = new Dictionary<Guid, decimal>();
            var physLatestByStudent = new Dictionary<Guid, decimal>();

            if (await reader.NextResultAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    var stId = reader.GetGuid(0);
                    var bucket = reader.GetInt32(1);
                    var rn = reader.GetInt64(2);
                    var totalNet = reader.GetDecimal(3);

                    if (bucket == 1)
                    {
                        if (rn == 1) mathLatestByStudent[stId] = totalNet;
                        else if (rn == 2) mathPrevByStudent[stId] = totalNet;
                    }
                    else if (bucket == 2 && rn == 1)
                    {
                        physLatestByStudent[stId] = totalNet;
                    }
                }
            }

            var studentNets = new List<TeacherStudentNetDto>(students.Count);
            int netCounter = 1;
            foreach (var s in students)
            {
                var initials = s.FirstName.Length > 0 && s.LastName.Length > 0
                    ? $"{s.FirstName[0]}{s.LastName[0]}"
                    : "ÖG";

                decimal? mathNet = mathLatestByStudent.TryGetValue(s.Id, out var mLatest)
                    ? Math.Round(mLatest, 1)
                    : null;
                decimal? physNet = physLatestByStudent.TryGetValue(s.Id, out var pLatest)
                    ? Math.Round(pLatest, 1)
                    : null;

                string trend = "stable";
                if (mathLatestByStudent.TryGetValue(s.Id, out var m0) &&
                    mathPrevByStudent.TryGetValue(s.Id, out var m1))
                {
                    var diff = m0 - m1;
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
                $"{teacherFirstName} {teacherLastName}",
                assignedSubjectNames,
                students.Count,
                totalAssignedClasses,
                academicGaps,
                studentNets
            );
        }

        // Non-relational fallback (e.g. InMemory test provider)
        var teacher = await db.Set<Teacher>().AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == user.UserId.Value && t.InstitutionId == institutionId.Value && !t.IsDeleted, ct)
            ?? throw new NotFoundException("Teacher profile not found for user.");

        var subjectIds = await db.Set<TeacherSubject>().AsNoTracking()
            .Where(ts => ts.TeacherId == teacher.Id && ts.InstitutionId == teacher.InstitutionId && !ts.IsDeleted)
            .Select(ts => ts.SubjectId)
            .Distinct()
            .ToListAsync(ct);

        var subjectNamesMap = await academic.GetSubjectNamesAsync(subjectIds, ct);
        var fallbackSubjectNames = subjectIds
            .Select(sid => subjectNamesMap.GetValueOrDefault(sid, "Bilinmeyen Ders"))
            .Distinct()
            .ToList();

        var classroomIds = await db.Set<ClassroomTeacher>().AsNoTracking()
            .Where(ctLink => ctLink.TeacherId == teacher.Id && ctLink.InstitutionId == teacher.InstitutionId && !ctLink.IsDeleted)
            .Select(ctLink => ctLink.ClassroomId)
            .Distinct()
            .ToListAsync(ct);

        var studentIds = await db.Set<ClassroomStudent>().AsNoTracking()
            .Where(cs => classroomIds.Contains(cs.ClassroomId) && cs.InstitutionId == teacher.InstitutionId && !cs.IsDeleted)
            .Select(cs => cs.StudentId)
            .Distinct()
            .ToListAsync(ct);

        if (studentIds.Count == 0)
        {
            return new TeacherDashboardDto(
                $"{teacher.FirstName} {teacher.LastName}",
                fallbackSubjectNames,
                0,
                classroomIds.Count,
                [],
                []
            );
        }

        var fallbackStudentNets = new List<TeacherStudentNetDto>(studentIds.Count);
        int fallbackCounter = 1;
        foreach (var sid in studentIds)
        {
            var shortId = sid.ToString()[..4];
            fallbackStudentNets.Add(new TeacherStudentNetDto(
                $"sn-{fallbackCounter++}",
                sid.ToString(),
                $"Öğrenci {shortId}",
                "11",
                null,
                null,
                "stable",
                $"Ö{shortId[0]}"
            ));
        }

        return new TeacherDashboardDto(
            $"{teacher.FirstName} {teacher.LastName}",
            fallbackSubjectNames,
            studentIds.Count,
            classroomIds.Count,
            [],
            fallbackStudentNets
        );
    }
}

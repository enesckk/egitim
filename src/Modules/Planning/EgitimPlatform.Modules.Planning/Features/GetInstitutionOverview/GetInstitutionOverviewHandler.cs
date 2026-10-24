using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features.GetInstitutionOverview;

public class GetInstitutionOverviewHandler(
    IApplicationDbContext db,
    ICurrentUser user)
{
    private sealed record RawAttemptSubjectResult(
        DateTimeOffset TakenAt,
        string SubjectName,
        decimal Net
    );

    public async Task<InstitutionOverviewDto> HandleAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            throw new ForbiddenException("Access denied.");

        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var targetInstId = institutionId ?? Guid.Empty;
        if (user.IsSuperAdmin && targetInstId == Guid.Empty)
        {
            // Fallback for superadmin without active institution header: query first active institution
            if (db is Microsoft.EntityFrameworkCore.DbContext superDb)
            {
                using var cmd = superDb.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT TOP 1 Id FROM Institutions WHERE IsDeleted = 0";
                if (cmd.Connection?.State != System.Data.ConnectionState.Open)
                    await cmd.Connection!.OpenAsync(ct);
                var scalar = await cmd.ExecuteScalarAsync(ct);
                if (scalar is Guid gid) targetInstId = gid;
            }
        }

        string institutionName = "Kurum Genel Bakış";
        int branchCount = 0;
        int activeStudents = 0;
        int activeCoaches = 0;
        decimal avgAdherence = 0m;
        decimal avgWeeklyStudyHours = 0m;
        int activePlanCount = 0;

        var coachesList = new List<AdminCoachPortfolioDto>();
        var trendAttempts = new List<RawAttemptSubjectResult>();

        if (db is Microsoft.EntityFrameworkCore.DbContext efDb && efDb.Database.IsRelational())
        {
            var conn = efDb.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            // 1. Institution Name & Branch (Classroom) Count
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT 
                        (SELECT TOP 1 Name FROM Institutions WHERE Id = '{targetInstId}') AS InstName,
                        (SELECT COUNT(*) FROM Classrooms WHERE InstitutionId = '{targetInstId}' AND IsDeleted = 0) AS BranchCount,
                        (SELECT COUNT(*) FROM Students WHERE InstitutionId = '{targetInstId}' AND IsDeleted = 0) AS StudentCount,
                        (SELECT COUNT(*) FROM Coaches WHERE InstitutionId = '{targetInstId}' AND IsDeleted = 0) AS CoachCount";

                using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    institutionName = reader.IsDBNull(0) ? "Kurum Genel Bakış" : reader.GetString(0);
                    branchCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    activeStudents = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    activeCoaches = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                }
            }

            // 2. Coach Portfolios with real adherence from StudyPlans
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT 
                        c.Id,
                        c.FirstName,
                        c.LastName,
                        COUNT(DISTINCT ca.StudentId) AS StudentCount,
                        ISNULL(AVG(p.UtilizationRate), 0) AS AvgAdherence,
                        ISNULL(AVG(p.TotalPlannedMinutes), 0) AS AvgMinutes
                    FROM Coaches c
                    LEFT JOIN StudentCoachAssignments ca ON ca.CoachId = c.Id AND ca.IsActive = 1
                    LEFT JOIN StudyPlans p ON p.StudentId = ca.StudentId AND p.Status = 1 AND p.IsDeleted = 0
                    WHERE c.InstitutionId = '{targetInstId}' AND c.IsDeleted = 0
                    GROUP BY c.Id, c.FirstName, c.LastName";

                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var cId = reader.GetGuid(0);
                    var fn = reader.GetString(1);
                    var ln = reader.GetString(2);
                    var stCount = reader.GetInt32(3);
                    var adhDecimal = reader.GetDecimal(4);
                    var avgMins = Convert.ToDecimal(reader.GetValue(5));

                    int adhPercent = (int)Math.Round(adhDecimal * 100m, MidpointRounding.AwayFromZero);
                    decimal avgHours = Math.Round(avgMins / 60m, 1);
                    string status = adhPercent >= 75 ? "good" : "attention";
                    int attentionCount = adhPercent < 75 ? 1 : 0;

                    var initials = fn.Length > 0 && ln.Length > 0 ? $"{fn[0]}{ln[0]}" : "KÇ";
                    coachesList.Add(new AdminCoachPortfolioDto(
                        cId.ToString(),
                        $"{fn} {ln}",
                        initials,
                        stCount,
                        adhPercent,
                        avgHours,
                        attentionCount,
                        status
                    ));
                }
            }

            // 4. Trend Data from StudentExamSubjectResults
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT 
                        a.TakenAt,
                        sub.Name AS SubjectName,
                        sr.Net
                    FROM StudentExamAttempts a
                    JOIN StudentExamSubjectResults sr ON sr.StudentExamAttemptId = a.Id AND sr.IsDeleted = 0
                    JOIN ExamSections es ON sr.ExamSectionId = es.Id
                    JOIN AcademicSubjects sub ON es.SubjectId = sub.Id
                    WHERE a.InstitutionId = '{targetInstId}' AND a.IsDeleted = 0 AND a.Status = 1
                    ORDER BY a.TakenAt ASC";

                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var takenAt = reader.GetFieldValue<DateTimeOffset>(0);
                    var subName = reader.GetString(1);
                    var net = reader.GetDecimal(2);
                    trendAttempts.Add(new RawAttemptSubjectResult(takenAt, subName, net));
                }
            }
        }

        // 3. Overall Study Plan Metrics (Queryable across providers)
        var activePlans = await db.Set<StudyPlan>().AsNoTracking()
            .Where(p => p.InstitutionId == targetInstId && p.Status == StudyPlanStatus.Active && !p.IsDeleted)
            .ToListAsync(ct);

        activePlanCount = activePlans.Count;
        if (activePlanCount > 0)
        {
            avgAdherence = Math.Round((decimal)activePlans.Average(p => (double)p.UtilizationRate) * 100m, 0);
            avgWeeklyStudyHours = Math.Round((decimal)activePlans.Average(p => (double)p.TotalPlannedMinutes) / 60m, 1);
        }

        // Build Trend Points grouped by Month or Exam period
        var trendPoints = new List<TytTrendPointDto>();
        var monthlyGroups = trendAttempts
            .GroupBy(t => new { Year = t.TakenAt.Year, Month = t.TakenAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .ToList();

        // Turkish month abbreviations
        string[] trMonths = ["", "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"];

        foreach (var mg in monthlyGroups)
        {
            var label = $"{trMonths[mg.Key.Month]} '{mg.Key.Year.ToString().Substring(2)}";
            var turkce = mg.Where(x => x.SubjectName.Contains("Türkçe")).Select(x => x.Net).DefaultIfEmpty(0m).Average();
            var matematik = mg.Where(x => x.SubjectName.Contains("Matematik")).Select(x => x.Net).DefaultIfEmpty(0m).Average();
            var fen = mg.Where(x => x.SubjectName.Contains("Fizik") || x.SubjectName.Contains("Kimya") || x.SubjectName.Contains("Biyoloji"))
                       .Select(x => x.Net).DefaultIfEmpty(0m).Average();
            var sosyal = mg.Where(x => x.SubjectName.Contains("Tarih") || x.SubjectName.Contains("Coğrafya") || x.SubjectName.Contains("Felsefe"))
                          .Select(x => x.Net).DefaultIfEmpty(0m).Average();

            trendPoints.Add(new TytTrendPointDto(
                label,
                Math.Round(turkce, 1),
                Math.Round(matematik, 1),
                Math.Round(sosyal, 1),
                Math.Round(fen, 1)
            ));
        }

        // Build 1m, 3m, 6m slices
        var trendData = new Dictionary<string, List<TytTrendPointDto>>
        {
            ["1m"] = trendPoints.TakeLast(4).ToList(),
            ["3m"] = trendPoints.TakeLast(6).ToList(),
            ["6m"] = trendPoints.TakeLast(12).ToList()
        };

        // KPI Grid
        var kpis = new List<InstitutionKpiDto>
        {
            new(
                "Aktif Öğrenci",
                activeStudents.ToString(),
                branchCount > 0 ? $"{branchCount} şubede" : "Kayıtlı",
                "bg-primary-50 text-primary-600",
                "users"
            ),
            new(
                "Aktif Koç",
                activeCoaches.ToString(),
                $"{coachesList.Count(c => c.Students > 0)} aktif portföy",
                "bg-navy-50 text-navy-600",
                "coaches"
            ),
            new(
                "Ortalama Plan Uyumu",
                activePlanCount > 0 ? $"%{avgAdherence:0}" : "%0",
                activePlanCount > 0 ? $"{activePlanCount} aktif planda" : "Henüz plan yok",
                "bg-success-light text-success",
                "adherence"
            ),
            new(
                "Haftalık Çalışma Ort.",
                activePlanCount > 0 ? $"{avgWeeklyStudyHours:0.0} sa" : "0 sa",
                "öğrenci başına",
                "bg-warning-light text-warning",
                "clock"
            )
        };

        // Attention Stats
        int lowAdherenceCount = coachesList.Sum(c => c.Attention);
        var attentionStats = new List<AttentionStatDto>
        {
            new("Kritik öğrenci", activeStudents > 0 ? "0" : "0", "7+ gün aktivite yok"),
            new("Düşük plan uyumu", lowAdherenceCount.ToString(), "<%75 bu hafta")
        };

        return new InstitutionOverviewDto(
            institutionName,
            branchCount,
            kpis,
            trendData,
            attentionStats,
            coachesList
        );
    }
}

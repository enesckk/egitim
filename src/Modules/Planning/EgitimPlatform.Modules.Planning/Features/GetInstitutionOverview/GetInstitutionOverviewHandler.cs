using System.Data;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features.GetInstitutionOverview;

public class GetInstitutionOverviewHandler(
    IApplicationDbContext db,
    ICurrentUser user)
{
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
            if (db is DbContext superDb && superDb.Database.IsRelational())
            {
                using var cmd = superDb.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT TOP (1) Id FROM Institutions WHERE IsDeleted = 0";
                if (cmd.Connection?.State != ConnectionState.Open)
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
        var trendPoints = new List<TytTrendPointDto>();
        string[] trMonths = ["", "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"];

        if (db is DbContext efDb && efDb.Database.IsRelational())
        {
            var conn = efDb.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    (SELECT TOP (1) Name FROM Institutions WHERE Id = @instId) AS InstName,
                    (SELECT COUNT(*) FROM Classrooms WHERE InstitutionId = @instId AND IsDeleted = 0) AS BranchCount,
                    (SELECT COUNT(*) FROM Students WHERE InstitutionId = @instId AND IsDeleted = 0) AS StudentCount,
                    (SELECT COUNT(*) FROM Coaches WHERE InstitutionId = @instId AND IsDeleted = 0) AS CoachCount,
                    COUNT(*) AS ActivePlanCount,
                    ISNULL(AVG(CAST(UtilizationRate AS float)), 0) AS AvgUtilization,
                    ISNULL(AVG(CAST(TotalPlannedMinutes AS float)), 0) AS AvgPlannedMinutes
                FROM StudyPlans
                WHERE InstitutionId = @instId AND Status = 1 AND IsDeleted = 0;

                SELECT 
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    COUNT(DISTINCT ca.StudentId) AS StudentCount,
                    ISNULL(AVG(p.UtilizationRate), 0) AS AvgAdherence,
                    ISNULL(AVG(p.TotalPlannedMinutes), 0) AS AvgMinutes
                FROM Coaches c
                LEFT JOIN StudentCoachAssignments ca ON ca.CoachId = c.Id AND ca.InstitutionId = @instId AND ca.IsActive = 1
                LEFT JOIN StudyPlans p ON p.StudentId = ca.StudentId AND p.InstitutionId = @instId AND p.Status = 1 AND p.IsDeleted = 0
                WHERE c.InstitutionId = @instId AND c.IsDeleted = 0
                GROUP BY c.Id, c.FirstName, c.LastName;

                ;WITH SubjectBuckets AS (
                    SELECT
                        es.Id AS ExamSectionId,
                        CASE
                            WHEN sub.Name LIKE N'%Türkçe%' THEN 1
                            WHEN sub.Name LIKE N'%Matematik%' THEN 2
                            WHEN sub.Name LIKE N'%Fizik%' OR sub.Name LIKE N'%Kimya%' OR sub.Name LIKE N'%Biyoloji%' THEN 3
                            WHEN sub.Name LIKE N'%Tarih%' OR sub.Name LIKE N'%Coğrafya%' OR sub.Name LIKE N'%Felsefe%' THEN 4
                            ELSE 0
                        END AS Bucket
                    FROM ExamSections es
                    INNER JOIN AcademicSubjects sub ON sub.Id = es.SubjectId AND sub.IsDeleted = 0
                    WHERE es.InstitutionId = @instId AND es.IsDeleted = 0
                )
                SELECT TOP (12)
                    YEAR(a.TakenAt) AS AttemptYear,
                    MONTH(a.TakenAt) AS AttemptMonth,
                    ISNULL(AVG(CASE WHEN sb.Bucket = 1 THEN CAST(sr.Net AS decimal(18,6)) END), 0) AS TurkceAvg,
                    ISNULL(AVG(CASE WHEN sb.Bucket = 2 THEN CAST(sr.Net AS decimal(18,6)) END), 0) AS MatematikAvg,
                    ISNULL(AVG(CASE WHEN sb.Bucket = 3 THEN CAST(sr.Net AS decimal(18,6)) END), 0) AS FenAvg,
                    ISNULL(AVG(CASE WHEN sb.Bucket = 4 THEN CAST(sr.Net AS decimal(18,6)) END), 0) AS SosyalAvg
                FROM StudentExamAttempts a
                INNER JOIN StudentExamSubjectResults sr ON sr.StudentExamAttemptId = a.Id AND sr.IsDeleted = 0
                INNER JOIN SubjectBuckets sb ON sb.ExamSectionId = sr.ExamSectionId AND sb.Bucket > 0
                WHERE a.InstitutionId = @instId AND a.IsDeleted = 0 AND a.Status = 1
                GROUP BY YEAR(a.TakenAt), MONTH(a.TakenAt)
                ORDER BY AttemptYear DESC, AttemptMonth DESC;";

            var instParam = cmd.CreateParameter();
            instParam.ParameterName = "@instId";
            instParam.DbType = DbType.Guid;
            instParam.Value = targetInstId;
            cmd.Parameters.Add(instParam);

            using var reader = await cmd.ExecuteReaderAsync(ct);

            // Resultset 1: Overview & Study Plan KPIs
            if (await reader.ReadAsync(ct))
            {
                institutionName = reader.IsDBNull(0) ? "Kurum Genel Bakış" : reader.GetString(0);
                branchCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                activeStudents = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                activeCoaches = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                activePlanCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                if (activePlanCount > 0)
                {
                    var avgUtil = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5);
                    var avgMins = reader.IsDBNull(6) ? 0.0 : reader.GetDouble(6);
                    avgAdherence = Math.Round((decimal)avgUtil * 100m, 0);
                    avgWeeklyStudyHours = Math.Round((decimal)avgMins / 60m, 1);
                }
            }

            // Resultset 2: Coach portfolios
            if (await reader.NextResultAsync(ct))
            {
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

            // Resultset 3: Monthly subject net averages (ordered DESC in SQL, reversed to ASC here)
            if (await reader.NextResultAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    int year = reader.GetInt32(0);
                    int month = reader.GetInt32(1);
                    decimal turkce = reader.GetDecimal(2);
                    decimal matematik = reader.GetDecimal(3);
                    decimal fen = reader.GetDecimal(4);
                    decimal sosyal = reader.GetDecimal(5);

                    var label = $"{trMonths[month]} '{year.ToString()[2..]}";
                    trendPoints.Add(new TytTrendPointDto(
                        label,
                        Math.Round(turkce, 1),
                        Math.Round(matematik, 1),
                        Math.Round(sosyal, 1),
                        Math.Round(fen, 1)
                    ));
                }
                trendPoints.Reverse();
            }
        }
        else
        {
            // Non-relational fallback (e.g. InMemory test provider)
            var activePlans = await db.Set<StudyPlan>().AsNoTracking()
                .Where(p => p.InstitutionId == targetInstId && p.Status == StudyPlanStatus.Active && !p.IsDeleted)
                .Select(p => new { p.UtilizationRate, p.TotalPlannedMinutes })
                .ToListAsync(ct);

            activePlanCount = activePlans.Count;
            if (activePlanCount > 0)
            {
                avgAdherence = Math.Round((decimal)activePlans.Average(p => (double)p.UtilizationRate) * 100m, 0);
                avgWeeklyStudyHours = Math.Round((decimal)activePlans.Average(p => (double)p.TotalPlannedMinutes) / 60m, 1);
            }
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

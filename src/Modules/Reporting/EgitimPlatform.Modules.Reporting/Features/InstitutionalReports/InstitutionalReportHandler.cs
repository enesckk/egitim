using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Reporting.Features.InstitutionalReports;

public interface IInstitutionalReportHandler
{
    Task<InstitutionalReportDto> GetReportDataAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<(byte[] content, string contentType, string fileName)> ExportReportAsync(ReportFilterRequest filter, string format, CancellationToken ct = default);
}

public class InstitutionalReportHandler : IInstitutionalReportHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public InstitutionalReportHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private async Task<Guid> GetAuthorizedInstitutionIdAsync()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            throw new ForbiddenException("Erişim reddedildi. Oturum açılmalıdır.");

        var hasAccess = _currentUser.Roles.Contains(Roles.SuperAdmin) ||
                        _currentUser.Roles.Contains(Roles.InstitutionAdmin);

        if (!hasAccess)
            throw new ForbiddenException("Kurum raporlarına sadece Kurum Yöneticisi erişebilir.");

        var instId = await _currentUser.GetInstitutionIdAsync();
        if (!instId.HasValue && !_currentUser.IsSuperAdmin)
            throw new ForbiddenException("Kurum bilgisi bulunamadı.");

        var targetInstId = instId ?? Guid.Empty;
        if (_currentUser.IsSuperAdmin && targetInstId == Guid.Empty)
        {
            if (_db is DbContext superDb)
            {
                using var cmd = superDb.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT TOP 1 Id FROM Institutions WHERE IsDeleted = 0";
                if (cmd.Connection?.State != System.Data.ConnectionState.Open)
                    await cmd.Connection!.OpenAsync();
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar is Guid gid) targetInstId = gid;
            }
        }

        return targetInstId;
    }

    public async Task<InstitutionalReportDto> GetReportDataAsync(ReportFilterRequest filter, CancellationToken ct = default)
    {
        var instId = await GetAuthorizedInstitutionIdAsync();

        if (_db is not DbContext efDb || !efDb.Database.IsRelational())
        {
            return new InstitutionalReportDto(
                "Kurum Raporu", 0, 0, 0, 0, 0,
                new List<BranchReportItemDto>(),
                new List<AdherenceDistributionDto>(),
                new List<SubjectAverageDto>(),
                new List<ExamPerformanceRowDto>()
            );
        }

        var conn = efDb.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        // 1. Institution Name
        string institutionName = "Kurum Raporu";
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT TOP 1 Name FROM Institutions WHERE Id = @instId";
            var p = cmd.CreateParameter();
            p.ParameterName = "@instId";
            p.Value = instId;
            cmd.Parameters.Add(p);

            var scalar = await cmd.ExecuteScalarAsync(ct);
            if (scalar is string s) institutionName = s;
        }

        // 2. Classrooms & Branches
        var branches = new List<BranchReportItemDto>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT 
                    c.Id,
                    c.Name,
                    COUNT(DISTINCT cs.StudentId) AS StudentCount,
                    COUNT(DISTINCT ct.TeacherId) AS TeacherCount,
                    ISNULL(AVG(p.UtilizationRate), 0) AS AvgAdherence,
                    ISNULL(AVG(a.TotalNet), 0) AS AvgNet
                FROM Classrooms c
                LEFT JOIN ClassroomStudents cs ON cs.ClassroomId = c.Id AND cs.InstitutionId = @instId AND cs.IsDeleted = 0
                LEFT JOIN ClassroomTeachers ct ON ct.ClassroomId = c.Id AND ct.InstitutionId = @instId AND ct.IsDeleted = 0
                LEFT JOIN StudyPlans p ON p.StudentId = cs.StudentId AND p.InstitutionId = @instId AND p.Status = 1 AND p.IsDeleted = 0
                LEFT JOIN StudentExamAttempts a ON a.StudentId = cs.StudentId AND a.InstitutionId = @instId AND a.IsDeleted = 0
                WHERE c.InstitutionId = @instId AND c.IsDeleted = 0
                GROUP BY c.Id, c.Name
                ORDER BY c.Name";

            var p = cmd.CreateParameter();
            p.ParameterName = "@instId";
            p.Value = instId;
            cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var cId = reader.GetGuid(0);
                var cName = reader.GetString(1);
                var sCount = reader.GetInt32(2);
                var tCount = reader.GetInt32(3);
                var avgAdh = Convert.ToDecimal(reader.GetValue(4));
                var avgNet = Convert.ToDecimal(reader.GetValue(5));

                branches.Add(new BranchReportItemDto(
                    cId,
                    cName,
                    sCount,
                    tCount,
                    Math.Round(avgAdh, 1),
                    Math.Round(avgNet, 2)
                ));
            }
        }

        // 3. Adherence Distribution across all active study plans in institution
        var adherenceDist = new List<AdherenceDistributionDto>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT 
                    COUNT(CASE WHEN UtilizationRate >= 80 THEN 1 END) AS HighCount,
                    COUNT(CASE WHEN UtilizationRate >= 50 AND UtilizationRate < 80 THEN 1 END) AS MidCount,
                    COUNT(CASE WHEN UtilizationRate < 50 THEN 1 END) AS LowCount,
                    COUNT(*) AS TotalCount
                FROM StudyPlans
                WHERE InstitutionId = @instId AND Status = 1 AND IsDeleted = 0";

            var p = cmd.CreateParameter();
            p.ParameterName = "@instId";
            p.Value = instId;
            cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var high = reader.GetInt32(0);
                var mid = reader.GetInt32(1);
                var low = reader.GetInt32(2);
                var total = reader.GetInt32(3);

                decimal hPct = total > 0 ? Math.Round((decimal)high * 100 / total, 1) : 0;
                decimal mPct = total > 0 ? Math.Round((decimal)mid * 100 / total, 1) : 0;
                decimal lPct = total > 0 ? Math.Round((decimal)low * 100 / total, 1) : 0;

                adherenceDist.Add(new AdherenceDistributionDto("Yüksek Uyuma Sahip (≥%80)", high, hPct, "bg-success"));
                adherenceDist.Add(new AdherenceDistributionDto("Orta Düzey Uyum (%50–%79)", mid, mPct, "bg-warning"));
                adherenceDist.Add(new AdherenceDistributionDto("Kritik Düşük Uyum (<%50)", low, lPct, "bg-danger"));
            }
        }

        // 4. Subject Averages with applied filters (Classroom, Exam, Dates)
        var subjectAverages = new List<SubjectAverageDto>();
        using (var cmd = conn.CreateCommand())
        {
            var sql = new StringBuilder(@"
                SELECT 
                    sub.Name AS SubjectName,
                    AVG(sr.Net) AS AvgNet,
                    AVG(CAST(sr.Correct AS decimal(10,2))) AS AvgCorrect,
                    AVG(CAST(sr.Wrong AS decimal(10,2))) AS AvgWrong
                FROM StudentExamSubjectResults sr
                JOIN StudentExamAttempts a ON a.Id = sr.StudentExamAttemptId AND a.IsDeleted = 0
                JOIN ExamSections es ON es.Id = sr.ExamSectionId AND es.IsDeleted = 0
                JOIN AcademicSubjects sub ON sub.Id = es.SubjectId AND sub.IsDeleted = 0
                LEFT JOIN ClassroomStudents cs ON cs.StudentId = a.StudentId AND cs.IsDeleted = 0
                WHERE sr.InstitutionId = @instId AND sr.IsDeleted = 0");

            var p = cmd.CreateParameter();
            p.ParameterName = "@instId";
            p.Value = instId;
            cmd.Parameters.Add(p);

            if (filter.ClassroomId.HasValue && filter.ClassroomId.Value != Guid.Empty)
            {
                sql.Append(" AND cs.ClassroomId = @classroomId");
                var pc = cmd.CreateParameter();
                pc.ParameterName = "@classroomId";
                pc.Value = filter.ClassroomId.Value;
                cmd.Parameters.Add(pc);
            }

            if (filter.ExamId.HasValue && filter.ExamId.Value != Guid.Empty)
            {
                sql.Append(" AND a.ExamId = @examId");
                var pe = cmd.CreateParameter();
                pe.ParameterName = "@examId";
                pe.Value = filter.ExamId.Value;
                cmd.Parameters.Add(pe);
            }

            if (filter.StartDate.HasValue)
            {
                sql.Append(" AND a.CreatedAt >= @startDate");
                var ps = cmd.CreateParameter();
                ps.ParameterName = "@startDate";
                ps.Value = filter.StartDate.Value;
                cmd.Parameters.Add(ps);
            }

            if (filter.EndDate.HasValue)
            {
                sql.Append(" AND a.CreatedAt <= @endDate");
                var pe = cmd.CreateParameter();
                pe.ParameterName = "@endDate";
                pe.Value = filter.EndDate.Value;
                cmd.Parameters.Add(pe);
            }

            sql.Append(" GROUP BY sub.Name ORDER BY sub.Name");
            cmd.CommandText = sql.ToString();

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var sName = reader.GetString(0);
                var aNet = Convert.ToDecimal(reader.GetValue(1));
                var aCor = Convert.ToDecimal(reader.GetValue(2));
                var aWro = Convert.ToDecimal(reader.GetValue(3));

                subjectAverages.Add(new SubjectAverageDto(
                    sName,
                    Math.Round(aNet, 2),
                    Math.Round(aCor, 1),
                    Math.Round(aWro, 1)
                ));
            }
        }

        // 5. Exam Performance Rows (for drill-down and export)
        var examPerformances = new List<ExamPerformanceRowDto>();
        using (var cmd = conn.CreateCommand())
        {
            var sql = new StringBuilder(@"
                SELECT 
                    a.Id,
                    e.Title AS ExamTitle,
                    ISNULL(c.Name, 'Genel') AS ClassroomName,
                    (st.FirstName + ' ' + st.LastName) AS StudentName,
                    a.TotalNet,
                    ISNULL(a.ReportedScore, 0) AS Score,
                    a.CreatedAt
                FROM StudentExamAttempts a
                JOIN Exams e ON e.Id = a.ExamId AND e.IsDeleted = 0
                JOIN Students st ON st.Id = a.StudentId AND st.IsDeleted = 0
                LEFT JOIN ClassroomStudents cs ON cs.StudentId = st.Id AND cs.IsDeleted = 0
                LEFT JOIN Classrooms c ON c.Id = cs.ClassroomId AND c.IsDeleted = 0
                WHERE a.InstitutionId = @instId AND a.IsDeleted = 0");

            var p = cmd.CreateParameter();
            p.ParameterName = "@instId";
            p.Value = instId;
            cmd.Parameters.Add(p);

            if (filter.ClassroomId.HasValue && filter.ClassroomId.Value != Guid.Empty)
            {
                sql.Append(" AND cs.ClassroomId = @classroomId");
                var pc = cmd.CreateParameter();
                pc.ParameterName = "@classroomId";
                pc.Value = filter.ClassroomId.Value;
                cmd.Parameters.Add(pc);
            }

            if (filter.ExamId.HasValue && filter.ExamId.Value != Guid.Empty)
            {
                sql.Append(" AND a.ExamId = @examId");
                var pe = cmd.CreateParameter();
                pe.ParameterName = "@examId";
                pe.Value = filter.ExamId.Value;
                cmd.Parameters.Add(pe);
            }

            if (filter.StartDate.HasValue)
            {
                sql.Append(" AND a.CreatedAt >= @startDate");
                var ps = cmd.CreateParameter();
                ps.ParameterName = "@startDate";
                ps.Value = filter.StartDate.Value;
                cmd.Parameters.Add(ps);
            }

            if (filter.EndDate.HasValue)
            {
                sql.Append(" AND a.CreatedAt <= @endDate");
                var pe = cmd.CreateParameter();
                pe.ParameterName = "@endDate";
                pe.Value = filter.EndDate.Value;
                cmd.Parameters.Add(pe);
            }

            sql.Append(" ORDER BY a.CreatedAt DESC");
            cmd.CommandText = sql.ToString();

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                examPerformances.Add(new ExamPerformanceRowDto(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDecimal(4),
                    reader.GetDecimal(5),
                    (DateTimeOffset)reader.GetValue(6)
                ));
            }
        }

        // Summary Calculations
        int totalStudents = branches.Sum(b => b.StudentCount);
        int totalAttempts = examPerformances.Count;
        int totalExams = examPerformances.Select(p => p.ExamTitle).Distinct().Count();
        decimal overallAvgNet = totalAttempts > 0 ? Math.Round(examPerformances.Average(p => p.TotalNet), 2) : 0m;
        decimal overallAvgAdh = branches.Any(b => b.AverageAdherence > 0)
            ? Math.Round(branches.Where(b => b.AverageAdherence > 0).Average(b => b.AverageAdherence), 1)
            : 0m;

        return new InstitutionalReportDto(
            institutionName,
            totalStudents,
            totalExams,
            totalAttempts,
            overallAvgNet,
            overallAvgAdh,
            branches,
            adherenceDist,
            subjectAverages,
            examPerformances
        );
    }

    public async Task<(byte[] content, string contentType, string fileName)> ExportReportAsync(ReportFilterRequest filter, string format, CancellationToken ct = default)
    {
        var data = await GetReportDataAsync(filter, ct);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmm");

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sinav Adi,Sube,Ogrenci,Net,Puan,Tarih");

            foreach (var row in data.ExamPerformances)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "\"{0}\",\"{1}\",\"{2}\",{3:F2},{4:F2},\"{5:yyyy-MM-dd HH:mm}\"",
                    SanitizeFormulaInjection(row.ExamTitle),
                    SanitizeFormulaInjection(row.ClassroomName),
                    SanitizeFormulaInjection(row.StudentName),
                    row.TotalNet,
                    row.Score,
                    row.TakenAt
                ));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return (bytes, "text/csv; charset=utf-8", $"Kurum_Raporu_{timestamp}.csv");
        }
        else
        {
            // XLSX export via ClosedXML
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Performans Raporu");

            // Header Row
            ws.Cell(1, 1).Value = "Sınav Adı";
            ws.Cell(1, 2).Value = "Şube";
            ws.Cell(1, 3).Value = "Öğrenci";
            ws.Cell(1, 4).Value = "Toplam Net";
            ws.Cell(1, 5).Value = "Puan";
            ws.Cell(1, 6).Value = "Sınav Tarihi";

            var headerRange = ws.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
            headerRange.Style.Font.FontColor = XLColor.White;

            int rowIdx = 2;
            foreach (var row in data.ExamPerformances)
            {
                ws.Cell(rowIdx, 1).Value = SanitizeFormulaInjection(row.ExamTitle);
                ws.Cell(rowIdx, 2).Value = SanitizeFormulaInjection(row.ClassroomName);
                ws.Cell(rowIdx, 3).Value = SanitizeFormulaInjection(row.StudentName);
                ws.Cell(rowIdx, 4).Value = (double)row.TotalNet;
                ws.Cell(rowIdx, 5).Value = (double)row.Score;
                ws.Cell(rowIdx, 6).Value = row.TakenAt.ToString("yyyy-MM-dd HH:mm");
                rowIdx++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Kurum_Raporu_{timestamp}.xlsx");
        }
    }

    private static string SanitizeFormulaInjection(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var trimmed = value.Trim();
        if (trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@') || trimmed.StartsWith('\t') || trimmed.StartsWith('\r'))
        {
            return "'" + trimmed;
        }
        return trimmed;
    }
}

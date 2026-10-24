namespace EgitimPlatform.Modules.Teachers.Features.GetTeacherDashboard;

public record TeacherDashboardDto(
    string TeacherName,
    List<string> AssignedSubjects,
    int TotalAssignedStudents,
    int TotalAssignedClasses,
    List<TeacherAcademicGapDto> AcademicGaps,
    List<TeacherStudentNetDto> StudentNets
);

public record TeacherAcademicGapDto(
    string Id,
    string StudentId,
    string StudentName,
    string Grade,
    string Subject,
    string Topic,
    string Detail,
    string Severity,
    string Initials,
    string LastExam
);

public record TeacherStudentNetDto(
    string Id,
    string StudentId,
    string Name,
    string Grade,
    decimal? MathNet,
    decimal? PhysicsNet,
    string Trend,
    string Initials
);

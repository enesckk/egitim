namespace EgitimPlatform.Modules.Planning.Features.GetInstitutionOverview;

public record InstitutionOverviewDto(
    string InstitutionName,
    int BranchCount,
    List<InstitutionKpiDto> Kpis,
    Dictionary<string, List<TytTrendPointDto>> TrendData,
    List<AttentionStatDto> AttentionStats,
    List<AdminCoachPortfolioDto> Coaches
);

public record InstitutionKpiDto(
    string Label,
    string Value,
    string Sub,
    string Color,
    string IconType
);

public record TytTrendPointDto(
    string Ay,
    decimal Turkce,
    decimal Matematik,
    decimal Sosyal,
    decimal Fen
);

public record AttentionStatDto(
    string Label,
    string Value,
    string Sub
);

public record AdminCoachPortfolioDto(
    string Id,
    string Name,
    string Initials,
    int Students,
    int Adherence,
    decimal AvgStudy,
    int Attention,
    string Status
);

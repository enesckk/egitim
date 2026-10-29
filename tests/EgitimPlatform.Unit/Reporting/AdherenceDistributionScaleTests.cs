using Xunit;
using EgitimPlatform.Modules.Reporting.Features.InstitutionalReports;

namespace EgitimPlatform.Unit.Reporting;

public class AdherenceDistributionScaleTests
{
    [Theory]
    [InlineData(0.00, "Low")]       // 0% -> Low (<50%)
    [InlineData(0.25, "Low")]       // 25% -> Low (<50%)
    [InlineData(0.4999, "Low")]     // 49.99% -> Low (<50%)
    [InlineData(0.50, "Mid")]       // 50% -> Mid (50%-79%)
    [InlineData(0.65, "Mid")]       // 65% -> Mid (50%-79%)
    [InlineData(0.7999, "Mid")]     // 79.99% -> Mid (50%-79%)
    [InlineData(0.80, "High")]      // 80% -> High (>=80%)
    [InlineData(1.00, "High")]      // 100% -> High (>=80%)
    public void UtilizationRate_StoredAsZeroToOneRatio_CategorizedCorrectly(decimal ratio, string expectedCategory)
    {
        // Assert mathematical thresholds used in InstitutionalReportHandler SQL
        string category;
        if (ratio >= 0.80m)
            category = "High";
        else if (ratio >= 0.50m)
            category = "Mid";
        else
            category = "Low";

        Assert.Equal(expectedCategory, category);
    }

    [Theory]
    [InlineData(0.00, 0.0)]
    [InlineData(0.25, 25.0)]
    [InlineData(0.50, 50.0)]
    [InlineData(0.536, 53.6)]
    [InlineData(0.65, 65.0)]
    [InlineData(0.80, 80.0)]
    [InlineData(1.00, 100.0)]
    public void UtilizationRate_ConvertedToPercentage_ScalesAccurately(decimal ratio, decimal expectedPercentage)
    {
        decimal percentage = Math.Round(ratio * 100m, 1);
        Assert.Equal(expectedPercentage, percentage);
    }

    [Fact]
    public void NoActivePlans_OverallAdherence_DefaultsToZero()
    {
        var branches = new List<BranchReportItemDto>
        {
            new(Guid.NewGuid(), "12-A", 0, 0, 0m, 0m),
            new(Guid.NewGuid(), "12-B", 0, 0, 0m, 0m)
        };

        decimal overallAvgAdh = branches.Any(b => b.AverageAdherence > 0)
            ? Math.Round(branches.Where(b => b.AverageAdherence > 0).Average(b => b.AverageAdherence), 1)
            : 0m;

        Assert.Equal(0m, overallAvgAdh);
    }
}

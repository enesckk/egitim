using Xunit;
using EgitimPlatform.Modules.Academic.Features;
using FluentAssertions;

namespace EgitimPlatform.Unit;

public class Sprint11PerformanceUnitTests
{
    [Fact]
    public void TaxonomyQuery_HasDefaultPageAndPageSize()
    {
        var query = new TaxonomyQuery("subjects", null);
        query.Level.Should().Be("subjects");
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(100);
    }

    [Fact]
    public void TaxonomyQueryValidator_ValidatesSupportedLevels()
    {
        var validator = new TaxonomyQueryValidator();
        var validQuery = new TaxonomyQuery("exam-types", null, 1, 50);
        var result = validator.Validate(validQuery);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TaxonomyQueryValidator_FailsOnInvalidLevel()
    {
        var validator = new TaxonomyQueryValidator();
        var invalidQuery = new TaxonomyQuery("invalid-level", null, 1, 50);
        var result = validator.Validate(invalidQuery);
        result.IsValid.Should().BeFalse();
    }
}

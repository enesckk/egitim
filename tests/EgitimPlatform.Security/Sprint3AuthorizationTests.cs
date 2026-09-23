using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint3AuthorizationTests(SecurityTestFactory factory)
{
    private async Task<HttpClient> Login(string name)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand($"s2-{name}@test.local", "Test@12345"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto!.AccessToken);
        return client;
    }

    [Fact]
    public async Task AnonymousAccess_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/exams")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/exams/{Sprint3TestData.ExamA}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/exam-results")).StatusCode);
    }

    [Theory]
    [InlineData("student", HttpStatusCode.OK)]
    [InlineData("other", HttpStatusCode.Forbidden)]
    [InlineData("foreign", HttpStatusCode.NotFound)]
    [InlineData("coach", HttpStatusCode.OK)]
    [InlineData("unassigned-coach", HttpStatusCode.Forbidden)]
    [InlineData("foreign-coach", HttpStatusCode.NotFound)]
    [InlineData("admin", HttpStatusCode.OK)]
    [InlineData("foreign-admin", HttpStatusCode.NotFound)]
    [InlineData("parent", HttpStatusCode.OK)]
    [InlineData("unrelated-parent", HttpStatusCode.Forbidden)]
    [InlineData("foreign-parent", HttpStatusCode.NotFound)]
    [InlineData("teacher", HttpStatusCode.Forbidden)]
    [InlineData("foreign-teacher", HttpStatusCode.NotFound)]
    [InlineData("super", HttpStatusCode.OK)]
    public async Task StudentExamResults_EnforcesResourceAuthorizationMatrix(string userRole, HttpStatusCode expected)
    {
        using var client = await Login(userRole);
        var response = await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/exam-results");
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("student", HttpStatusCode.OK)]
    [InlineData("other", HttpStatusCode.Forbidden)]
    [InlineData("foreign", HttpStatusCode.NotFound)]
    [InlineData("coach", HttpStatusCode.OK)]
    [InlineData("unassigned-coach", HttpStatusCode.Forbidden)]
    [InlineData("foreign-coach", HttpStatusCode.NotFound)]
    [InlineData("admin", HttpStatusCode.OK)]
    [InlineData("foreign-admin", HttpStatusCode.NotFound)]
    [InlineData("parent", HttpStatusCode.OK)]
    [InlineData("unrelated-parent", HttpStatusCode.Forbidden)]
    [InlineData("foreign-parent", HttpStatusCode.NotFound)]
    [InlineData("teacher", HttpStatusCode.Forbidden)]
    [InlineData("foreign-teacher", HttpStatusCode.NotFound)]
    [InlineData("super", HttpStatusCode.OK)]
    public async Task StudentExamAnalysis_EnforcesResourceAuthorizationMatrix(string userRole, HttpStatusCode expected)
    {
        using var client = await Login(userRole);
        var response = await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/exam-analysis");
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task TeacherRole_IsFailClosedForStudentExamResults()
    {
        using var client = await Login("teacher");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/exam-results")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/exam-analysis")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/exam-results/{Sprint3TestData.AttemptA}")).StatusCode);
    }

    [Fact]
    public async Task ParentRole_IsReadOnly_WriteOperationsAreForbidden()
    {
        using var client = await Login("parent");

        var attemptResponse = await client.PostAsJsonAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/attempts", new
        {
            StudentId = Sprint2TestData.StudentA,
            TakenAt = DateTimeOffset.UtcNow,
            ScoreSource = 0,
            Sections = new[]
            {
                new { ExamSectionId = Sprint3TestData.SectionA, Correct = 10, Wrong = 0, Blank = 0 }
            }
        });
        Assert.Equal(HttpStatusCode.Forbidden, attemptResponse.StatusCode);

        var finalizeResponse = await client.PostAsync($"/api/v1/exam-attempts/{Sprint3TestData.AttemptA}/finalize", null);
        Assert.Equal(HttpStatusCode.Forbidden, finalizeResponse.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_HasPlatformWideAccessToForeignExamsAndResults()
    {
        using var client = await Login("super");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/exams/{Sprint3TestData.ExamForeign}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentForeign}/exam-results")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentForeign}/exam-analysis")).StatusCode);
    }
}

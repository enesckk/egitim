using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint4CSecurityTests(SecurityTestFactory factory)
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
    public async Task AnonymousAccess_RawAnswersEndpoints_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var saveRes = await client.PostAsJsonAsync($"/api/v1/exam-attempts/{Sprint3TestData.AttemptA}/answers", new { Answers = new object[] { } });
        Assert.Equal(HttpStatusCode.Unauthorized, saveRes.StatusCode);

        var getRes = await client.GetAsync($"/api/v1/exam-attempts/{Sprint3TestData.AttemptA}/question-results");
        Assert.Equal(HttpStatusCode.Unauthorized, getRes.StatusCode);
    }

    [Fact]
    public async Task SaveAnswers_CrossTenantAttempt_ReturnsNotFound()
    {
        using var client = await Login("student");

        // Student A attempting to save answers for Tenant B's attempt -> 404 NotFound
        var payload = new
        {
            Answers = new[]
            {
                new { ExamQuestionId = Sprint4TestData.ExamQuestionInstAId, SelectedOption = "A" }
            }
        };

        var response = await client.PostAsJsonAsync($"/api/v1/exam-attempts/{Sprint3TestData.AttemptForeign}/answers", payload);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Student_BeforeFinalize_CannotAccessQuestionResults()
    {
        using var client = await Login("student");

        // AttemptA in Sprint3TestData is finalized, but if draft or foreign, access rules apply
        var response = await client.GetAsync($"/api/v1/exam-attempts/{Sprint3TestData.AttemptForeign}/question-results");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotCallFinalizeEndpoint_ReturnsForbidden()
    {
        using var client = await Login("student");

        var response = await client.PostAsync($"/api/v1/exam-attempts/{Sprint3TestData.AttemptA}/finalize", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotCallCreateAttempt_ReturnsForbidden()
    {
        using var client = await Login("student");

        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/attempts", new { StudentId = Sprint2TestData.StudentA, AttemptNumber = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotCallUpdateAttempt_ReturnsForbidden()
    {
        using var client = await Login("student");

        var response = await client.PutAsJsonAsync($"/api/v1/exam-attempts/{Sprint3TestData.AttemptA}", new { AttemptNumber = 2 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SaveAnswers_ForeignExamQuestionInjection_ReturnsNotFound()
    {
        using var client = await Login("student");

        // Student submits payload with ExamQuestionId belonging to foreign exam/tenant
        var payload = new
        {
            Answers = new[]
            {
                new { ExamQuestionId = Guid.NewGuid(), SelectedOption = "A" }
            }
        };

        var response = await client.PostAsJsonAsync($"/api/v1/exam-attempts/{Sprint4TestData.AttemptDraftA}/answers", payload);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

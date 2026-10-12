using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint4BSecurityTests(SecurityTestFactory factory)
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

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/questions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/questions/{Sprint4TestData.QuestionInstAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/questions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/questions", new { })).StatusCode);
    }

    [Fact]
    public async Task GetQuestionById_CrossTenantAttempt_ReturnsNotFound()
    {
        using var client = await Login("admin");

        // Admin at Tenant A reading Tenant B's question -> 404 NotFound
        var foreignRes = await client.GetAsync($"/api/v1/questions/{Sprint4TestData.QuestionInstBId}");
        Assert.Equal(HttpStatusCode.NotFound, foreignRes.StatusCode);

        // Admin at Tenant A reading Tenant A's question -> 200 OK
        var ownRes = await client.GetAsync($"/api/v1/questions/{Sprint4TestData.QuestionInstAId}");
        Assert.Equal(HttpStatusCode.OK, ownRes.StatusCode);

        // Admin at Tenant A reading Global question -> 200 OK
        var globalRes = await client.GetAsync($"/api/v1/questions/{Sprint4TestData.QuestionGlobalId}");
        Assert.Equal(HttpStatusCode.OK, globalRes.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_CanAccessAnyTenantQuestion()
    {
        using var client = await Login("super");

        var response = await client.GetAsync($"/api/v1/questions/{Sprint4TestData.QuestionInstBId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddExamQuestion_CrossTenantQuestion_ReturnsNotFound()
    {
        using var client = await Login("admin");

        // Try linking Tenant B's question to Tenant A's exam
        var payload = new
        {
            ExamSectionId = Sprint3TestData.SectionA,
            QuestionId = Sprint4TestData.QuestionInstBId,
            QuestionNumber = 99,
            Order = 99
        };

        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/questions", payload);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListExamQuestions_StudentDto_StrictlyMasksCorrectOptionAndIsCorrect()
    {
        using var studentClient = await Login("student");

        var studentResponse = await studentClient.GetAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/questions");
        Assert.Equal(HttpStatusCode.OK, studentResponse.StatusCode);

        var studentJson = await studentResponse.Content.ReadAsStringAsync();

        // Security assertion: Zero leakage of answer key fields to student
        Assert.DoesNotContain("correctOption", studentJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isCorrect", studentJson, StringComparison.OrdinalIgnoreCase);

        // Verify Teacher / Admin sees answer key
        using var teacherClient = await Login("teacher");
        var teacherResponse = await teacherClient.GetAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/questions");
        Assert.Equal(HttpStatusCode.OK, teacherResponse.StatusCode);

        var teacherJson = await teacherResponse.Content.ReadAsStringAsync();
        Assert.Contains("correctOption", teacherJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("isCorrect", teacherJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateQuestion_LinkedQuestion_EnforcesHistoricalImmutability()
    {
        using var client = await Login("admin");

        // QuestionInstAId is linked to ExamA in Sprint4TestData
        var updatePayload = new
        {
            SubjectId = Sprint4TestData.SubjectAId,
            TopicId = Sprint4TestData.TopicAId,
            QuestionType = 1,
            Difficulty = 1,
            SourceType = 1,
            Content = "Updated content attempt",
            Options = new[]
            {
                new { OptionLabel = "A", Content = "Opt A", IsCorrect = true },
                new { OptionLabel = "B", Content = "Opt B", IsCorrect = false },
                new { OptionLabel = "C", Content = "Opt C", IsCorrect = false },
                new { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };

        var response = await client.PutAsJsonAsync($"/api/v1/questions/{Sprint4TestData.QuestionInstAId}", updatePayload);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CoachRole_CannotManageOrViewQuestions_ReturnsForbidden()
    {
        using var client = await Login("coach");

        var payload = new
        {
            SubjectId = Sprint4TestData.SubjectAId,
            TopicId = Sprint4TestData.TopicAId,
            QuestionType = 1,
            Difficulty = 1,
            SourceType = 1,
            Content = "Coach question attempt",
            Options = new[]
            {
                new { OptionLabel = "A", Content = "Opt A", IsCorrect = true },
                new { OptionLabel = "B", Content = "Opt B", IsCorrect = false },
                new { OptionLabel = "C", Content = "Opt C", IsCorrect = false },
                new { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/questions", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var getResponse = await client.GetAsync("/api/v1/questions");
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
    }

    [Theory]
    [InlineData("teacher")]
    [InlineData("admin")]
    public async Task TenantRoles_CannotUpdateGlobalQuestion_ReturnsForbidden(string role)
    {
        using var client = await Login(role);

        var payload = new
        {
            SubjectId = Sprint4TestData.SubjectAId,
            TopicId = Sprint4TestData.TopicAId,
            QuestionType = 1,
            Difficulty = 1,
            SourceType = 2,
            Content = "Tenant attempting to update global question",
            Options = new[]
            {
                new { OptionLabel = "A", Content = "Opt A", IsCorrect = true },
                new { OptionLabel = "B", Content = "Opt B", IsCorrect = false },
                new { OptionLabel = "C", Content = "Opt C", IsCorrect = false },
                new { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };

        var response = await client.PutAsJsonAsync($"/api/v1/questions/{Sprint4TestData.QuestionGlobalId}", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("teacher")]
    [InlineData("admin")]
    public async Task TenantRoles_CannotArchiveGlobalQuestion_ReturnsForbidden(string role)
    {
        using var client = await Login(role);

        var response = await client.PostAsync($"/api/v1/questions/{Sprint4TestData.QuestionGlobalId}/archive", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("teacher")]
    [InlineData("admin")]
    public async Task TenantRoles_CannotCreateGlobalQuestion_ReturnsForbidden(string role)
    {
        using var client = await Login(role);

        var payload = new
        {
            SubjectId = Sprint4TestData.SubjectAId,
            TopicId = Sprint4TestData.TopicAId,
            QuestionType = 1,
            Difficulty = 1,
            SourceType = 2,
            Content = "Tenant attempting to create global question",
            IsGlobal = true,
            Options = new[]
            {
                new { OptionLabel = "A", Content = "Opt A", IsCorrect = true },
                new { OptionLabel = "B", Content = "Opt B", IsCorrect = false },
                new { OptionLabel = "C", Content = "Opt C", IsCorrect = false },
                new { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/questions", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_CannotUpdatePublishedGlobalQuestion_ReturnsConflictDueToImmutability()
    {
        using var client = await Login("super");

        var payload = new
        {
            SubjectId = Sprint4TestData.SubjectAId,
            TopicId = Sprint4TestData.TopicAId,
            QuestionType = 1,
            Difficulty = 1,
            SourceType = 2,
            Content = "SuperAdmin attempting to update published global question",
            Options = new[]
            {
                new { OptionLabel = "A", Content = "Opt A", IsCorrect = true },
                new { OptionLabel = "B", Content = "Opt B", IsCorrect = false },
                new { OptionLabel = "C", Content = "Opt C", IsCorrect = false },
                new { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };

        var response = await client.PutAsJsonAsync($"/api/v1/questions/{Sprint4TestData.QuestionGlobalId}", payload);
        // Immutability takes precedence even for SuperAdmin
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddExamQuestion_ArchivedQuestion_ReturnsBadRequest()
    {
        using var client = await Login("admin");

        var payload = new
        {
            ExamSectionId = Sprint3TestData.SectionA,
            QuestionId = Sprint4TestData.QuestionArchivedId,
            QuestionNumber = 88,
            Order = 88
        };

        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Sprint3TestData.ExamA}/questions", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

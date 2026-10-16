using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class SprintBSecurityTests(SecurityTestFactory factory)
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
    public async Task QuestionStyleAnalytics_EnforcesStrictSecurityMatrix(string user, HttpStatusCode expected)
    {
        using var client = await Login(user);
        var response = await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/academic-intelligence/question-styles");
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
    public async Task QuestionStyleTrends_EnforcesStrictSecurityMatrix(string user, HttpStatusCode expected)
    {
        using var client = await Login(user);
        var response = await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/academic-intelligence/question-style-trends?windowSize=2");
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task QuestionStylesEndpoints_Unauthenticated_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var resAnalytics = await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/academic-intelligence/question-styles");
        Assert.Equal(HttpStatusCode.Unauthorized, resAnalytics.StatusCode);

        var resTrends = await client.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/academic-intelligence/question-style-trends");
        Assert.Equal(HttpStatusCode.Unauthorized, resTrends.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint5BSecurityTests(SecurityTestFactory factory)
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
    public async Task CommitEndpoint_Anonymous_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "importType");
        content.Add(new StringContent("hash123"), "fileHash");
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.xlsx");

        var response = await client.PostAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/commit", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CommitEndpoint_StudentRole_ReturnsForbidden()
    {
        using var client = await Login("student");

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "importType");
        content.Add(new StringContent("hash123"), "fileHash");
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.xlsx");

        var response = await client.PostAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/commit", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CommitEndpoint_CrossTenantExam_ReturnsNotFound()
    {
        using var client = await Login("admin");

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "importType");
        content.Add(new StringContent("hash123"), "fileHash");
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.xlsx");

        // Admin of Tenant A attempting to commit import for a non-existent / foreign Tenant B exam
        var response = await client.PostAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/commit", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

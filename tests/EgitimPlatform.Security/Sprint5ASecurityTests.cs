using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint5ASecurityTests(SecurityTestFactory factory)
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
    public async Task ImportEndpoints_Anonymous_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var templateRes = await client.GetAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/templates/aggregate");
        Assert.Equal(HttpStatusCode.Unauthorized, templateRes.StatusCode);

        var previewRes = await client.PostAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/preview", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.Unauthorized, previewRes.StatusCode);
    }

    [Fact]
    public async Task ImportEndpoints_StudentRole_ReturnsForbidden()
    {
        using var client = await Login("student");

        var templateRes = await client.GetAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/templates/aggregate");
        Assert.Equal(HttpStatusCode.Forbidden, templateRes.StatusCode);
    }

    [Fact]
    public async Task ImportPreview_CrossTenantExam_ReturnsNotFound()
    {
        using var client = await Login("admin");

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "importType");
        
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "test.xlsx");

        // Admin of Tenant A trying to preview import for non-existent / Tenant B exam
        var response = await client.PostAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/preview", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

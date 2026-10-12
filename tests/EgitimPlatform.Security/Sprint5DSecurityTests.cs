using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgitimPlatform.Modules.Exams.Features.CreateManualResult;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint5DSecurityTests(SecurityTestFactory factory)
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
    public async Task SingleManualResult_Anonymous_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var command = new CreateSingleManualResultCommand(
            Guid.NewGuid(),
            [ new ManualSectionResultInputDto(Guid.NewGuid(), 10, 0, 0) ]
        );

        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Guid.NewGuid()}/manual-results", command);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SingleManualResult_StudentRole_ReturnsForbidden()
    {
        using var client = await Login("student");

        var command = new CreateSingleManualResultCommand(
            Guid.NewGuid(),
            [ new ManualSectionResultInputDto(Guid.NewGuid(), 10, 0, 0) ]
        );

        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Guid.NewGuid()}/manual-results", command);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BulkManualResult_Anonymous_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var command = new CreateBulkManualResultCommand([
            new BulkStudentManualResultItemDto(Guid.NewGuid(), [ new ManualSectionResultInputDto(Guid.NewGuid(), 10, 0, 0) ])
        ]);

        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Guid.NewGuid()}/manual-results/bulk", command);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BulkManualResult_StudentRole_ReturnsForbidden()
    {
        using var client = await Login("student");

        var command = new CreateBulkManualResultCommand([
            new BulkStudentManualResultItemDto(Guid.NewGuid(), [ new ManualSectionResultInputDto(Guid.NewGuid(), 10, 0, 0) ])
        ]);

        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Guid.NewGuid()}/manual-results/bulk", command);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SingleManualResult_CrossTenantExam_ReturnsNotFound()
    {
        using var client = await Login("admin"); // Tenant A admin

        var command = new CreateSingleManualResultCommand(
            Guid.NewGuid(),
            [ new ManualSectionResultInputDto(Guid.NewGuid(), 10, 0, 0) ]
        );

        // Tenant B foreign exam ID from Sprint3TestData
        var response = await client.PostAsJsonAsync($"/api/v1/exams/{Sprint3TestData.ExamForeign}/manual-results", command);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

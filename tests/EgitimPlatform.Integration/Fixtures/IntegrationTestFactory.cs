using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Testcontainers.MsSql;
using Xunit;

namespace EgitimPlatform.Integration.Fixtures;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly string _dbName = $"EgitimPlatform_Integration_{Guid.NewGuid():N}";

    public string ConnectionString => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
    {
        InitialCatalog = _dbName
    }.ConnectionString;

    public const string TestIssuer = "TestIssuer";
    public const string TestAudience = "TestAudience";
    public const string TestSigningKey = "TestSigningKey_MustBeAtLeast32CharactersLong!!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                [$"{JwtSettings.SectionName}:Issuer"] = TestIssuer,
                [$"{JwtSettings.SectionName}:Audience"] = TestAudience,
                [$"{JwtSettings.SectionName}:SigningKey"] = TestSigningKey,
                [$"{JwtSettings.SectionName}:AccessTokenExpirationMinutes"] = "30",
                [$"{JwtSettings.SectionName}:RefreshTokenExpirationDays"] = "7",
                ["Bootstrap:Enabled"] = "true",
                ["Bootstrap:SuperAdminEmail"] = "superadmin@egitimplatform.local",
                ["Bootstrap:SuperAdminPassword"] = "SuperAdmin@123!",
                // P2-01: High rate limits for test determinism
                ["RateLimiting:LoginLimit"] = "100000",
                ["RateLimiting:RefreshLimit"] = "100000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations and re-add with test connection
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(ConnectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Override JWT bearer validation to use test settings
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = TestAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var seeder = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Identity.Infrastructure.IdentitySeeder>();
        await seeder.SeedRolesAsync();
        // Bootstrap SuperAdmin for tests (enabled via test config below)
        await seeder.SeedBootstrapSuperAdminAsync();

        await TestData.TestDataSeeder.SeedTestDataAsync(Services);
    }

    public new async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFactory>
{
}

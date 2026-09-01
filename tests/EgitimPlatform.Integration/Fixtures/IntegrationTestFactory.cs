using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Infrastructure;
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

    public string ConnectionString => _container.GetConnectionString();

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
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration and re-add with test connection
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(ConnectionString));

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

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Identity.Infrastructure.IdentitySeeder>();
        await seeder.SeedAsync();

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

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qala.Game.Matches.EntityFrameworkCore;

namespace Qala.Framework.Tests;

/// <summary>Boots the real Matches host (InMemory database and bus) to check composition: routes, auth, health.</summary>
public sealed class MatchesHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Matches", "Server=unused;Database=unused");
        builder.UseSetting("MessageBroker:Transport", "InMemory");
        builder.UseSetting("Auth:Authority", "http://localhost:5000/");
        builder.UseSetting("Auth:Audience", "matches-api");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<MatchesDbContext>>();
            services.RemoveAll<DbContextOptions<MatchesDbContext>>();
            var databaseName = $"host-{Guid.NewGuid():N}";
            services.AddDbContext<MatchesDbContext>(o => o.UseInMemoryDatabase(databaseName));
        });
    }
}

public class MatchesHostTests(MatchesHostFactory factory) : IClassFixture<MatchesHostFactory>
{
    [Fact]
    public async Task Health_ShouldReturnOk_WhenAnonymous()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/matches/list")]
    [InlineData("/matches/getbyid")]
    [InlineData("/hubs/match/negotiate?negotiateVersion=1")]
    public async Task ProtectedEndpoint_ShouldReturnUnauthorized_WhenNoToken(string path)
    {
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync(new Uri(path, UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("matches/getbyid", "POST")]
    [InlineData("matches/mine", "POST")]
    [InlineData("matches/list", "POST")]
    [InlineData("matches/challenge", "POST")]
    [InlineData("matches/challenge/accept", "POST")]
    [InlineData("matches/queue", "POST")]
    [InlineData("matches/queue", "DELETE")]
    [InlineData("matches/stats", "POST")]
    public void DynamicControllers_ShouldExposeAppServiceEndpoints_WhenHostStarts(string route, string verb)
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

        Assert.Contains(endpoints, e =>
            e.RoutePattern.RawText == route
            && e.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods.Contains(verb) == true);
    }

    [Fact]
    public void DynamicControllers_ShouldNotExposeNonAppServiceTypes_WhenHostStarts()
    {
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(routes, r => r.Contains("playservice", StringComparison.OrdinalIgnoreCase));
        Assert.All(routes.Where(r => r.StartsWith("matches", StringComparison.Ordinal)), r => Assert.DoesNotContain("Async", r, StringComparison.Ordinal));
    }
}

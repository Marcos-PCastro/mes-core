using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mes.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_endpoint_does_not_require_authentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}

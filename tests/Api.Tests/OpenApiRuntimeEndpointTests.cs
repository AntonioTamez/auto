using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

/// <summary>
/// Cubre un hallazgo de code review sobre spec-1-7 (publicar el contrato
/// OpenAPI): ningún test ejercitaba el endpoint runtime `/openapi/v1.json`
/// (<c>Program.cs:39-42</c>, gateado a `Development`). Si un futuro bump
/// rompe el pin de versión exacto entre `Microsoft.AspNetCore.OpenApi` y
/// `Microsoft.Extensions.ApiDescription.Server` (ver <c>Api.csproj</c>), el
/// `FileLoadException` que ese pin evita solo se manifiesta al servir este
/// endpoint -- <c>OpenApiSpecPublishingTests</c> solo cubre la generación
/// build-time, no este camino runtime.
/// </summary>
public class OpenApiRuntimeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiRuntimeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
    }

    [Fact]
    public async Task OpenApiEndpoint_InDevelopment_ReturnsValidDocumentWithHealthPath()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("openapi", out var openApiVersion), "El documento debe traer la propiedad 'openapi'.");
        Assert.StartsWith("3.", openApiVersion.GetString());

        Assert.True(root.TryGetProperty("paths", out var paths), "El documento debe traer la propiedad 'paths'.");
        Assert.True(paths.TryGetProperty("/health", out _), "El contrato runtime debe describir '/health' (spec 1.5).");
    }
}

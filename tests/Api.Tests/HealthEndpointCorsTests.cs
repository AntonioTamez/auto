using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

/// <summary>
/// Cubre las filas 1 y 2 del I/O &amp; Edge-Case Matrix de spec-1-5
/// (endpoint de health-check verificable de punta a punta): CORS headers
/// presentes para el origen permitido, ausentes para uno no listado. El
/// bloqueo real de un origen no listado lo hace el navegador, no el
/// servidor -- por eso el servidor sigue respondiendo 200 en ambos casos;
/// lo único que cambia es la presencia del header.
/// </summary>
public class HealthEndpointCorsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointCorsTests(WebApplicationFactory<Program> factory)
    {
        // appsettings.Development.json es donde vive el origen permitido
        // http://localhost:4200 (ver spec 1.5 Code Map) -- se fija el
        // ambiente explícitamente para no depender de ASPNETCORE_ENVIRONMENT
        // del runner de tests.
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
    }

    [Fact]
    public async Task Health_AllowedOrigin_ReturnsCorsHeader()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:4200");

        var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Health_DisallowedOrigin_RespondsWithoutCorsHeader()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);

        // El servidor igual responde 200 -- solo el navegador bloquea la
        // respuesta al no encontrar el header CORS.
        Assert.True(response.IsSuccessStatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Health_ReturnsBodyWithExpectedStatusAndTimestampFields()
    {
        // Contrato real del body -- si Program.cs renombrara `status`/
        // `timestamp` mañana, ningún otro test (CORS headers, o los mocks
        // de app.spec.ts que inventan su propio payload) lo detectaría; el
        // shell desplegado simplemente mostraría "undefined".
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("status", out var statusProperty), "El body de /health debe traer la propiedad 'status'.");
        Assert.Equal(JsonValueKind.String, statusProperty.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(statusProperty.GetString()));

        Assert.True(root.TryGetProperty("timestamp", out var timestampProperty), "El body de /health debe traer la propiedad 'timestamp'.");
        Assert.Equal(JsonValueKind.String, timestampProperty.ValueKind);
        Assert.True(DateTimeOffset.TryParse(timestampProperty.GetString(), out _), "'timestamp' debe ser una fecha parseable (ISO 8601).");
    }

    [Fact]
    public async Task Health_AllowedOrigin_ViaDoubleUnderscoreEnvVar_ReturnsCorsHeader()
    {
        // Cors__AllowedOrigins__0 es la convención real que usa Terraform
        // para inyectar el origen de la SWA al Container App (ver
        // infra/terraform/main.tf, env block) -- production nunca pasa por
        // appsettings.Development.json, así que ese binding necesita su
        // propio test, independiente del fixture de la clase (que fuerza
        // Development). Ambiente Production para no mezclar con el origen
        // localhost:4200 de appsettings.Development.json.
        const string EnvVarName = "Cors__AllowedOrigins__0";
        const string AllowedOrigin = "https://swa-auto-dev.example.net";

        Environment.SetEnvironmentVariable(EnvVarName, AllowedOrigin);
        try
        {
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

            var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", AllowedOrigin);

            var response = await client.SendAsync(request);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVarName, null);
        }
    }
}

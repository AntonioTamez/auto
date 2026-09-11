using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Api.Tests;

/// <summary>
/// Cubre el registro CONDICIONAL de OpenTelemetry (spec 1.8, monitoreo
/// básico de disponibilidad): Program.cs solo llama
/// <c>AddOpenTelemetry().UseAzureMonitor()</c> cuando
/// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> viene seteada, no vacía y
/// con formato bien formado. Sin la variable, o con un valor de formato
/// inválido, la app debe seguir arrancando sin excepción -- ese es el
/// caso que rompía el build de CI y `dotnet run` local antes del guard
/// (ver Spec Change Log de spec-1-8: `Host.StartAsync` crea el
/// `MeterProvider` de forma eager y lanza si no hay connection string, o
/// si su formato es inválido -- este último caso verificado empíricamente
/// en esta historia, ver Program.cs). La paralelización de esta clase ya
/// está desactivada a nivel de ensamblado en
/// <see cref="HealthEndpointCorsTests"/>.
/// </summary>
public class ApplicationInsightsWiringTests
{
    private const string EnvVarName = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    // Formato sintácticamente válido (GUID + endpoint reales de Azure
    // Monitor), pero no una connection string real -- UseAzureMonitor()
    // no valida conectividad al arrancar el host, solo formato, así que
    // esto basta para probar el registro condicional sin depender de
    // Azure real.
    private const string FakeValidConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;" +
        "IngestionEndpoint=https://eastus-0.in.applicationinsights.azure.com/;" +
        "LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/";

    [Fact]
    public async Task Wiring_ConnectionStringSet_RegistersOpenTelemetryInDiContainer()
    {
        // Con la variable seteada (mismo mecanismo que usa Terraform para
        // inyectarla al Container App, ver infra/terraform/main.tf), el
        // registro condicional de Program.cs debe activarse: el
        // TracerProvider de OpenTelemetry queda resuelto en el contenedor
        // DI del host.
        Environment.SetEnvironmentVariable(EnvVarName, FakeValidConnectionString);
        try
        {
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            Assert.True(response.IsSuccessStatusCode);

            var tracerProvider = factory.Services.GetService<TracerProvider>();
            Assert.NotNull(tracerProvider);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVarName, null);
        }
    }

    [Fact]
    public async Task Wiring_ConnectionStringMissing_StartsWithoutExceptionAndSkipsRegistration()
    {
        // Sin la variable, el guard condicional de Program.cs debe evitar
        // por completo la llamada a AddOpenTelemetry().UseAzureMonitor():
        // el host arranca normalmente (sin la excepción eager de
        // Host.StartAsync documentada en el Spec Change Log) y ningún
        // TracerProvider queda registrado en el contenedor DI.
        Environment.SetEnvironmentVariable(EnvVarName, null);

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.True(response.IsSuccessStatusCode);

        var tracerProvider = factory.Services.GetService<TracerProvider>();
        Assert.Null(tracerProvider);
    }

    [Fact]
    public async Task Wiring_ConnectionStringMalformed_StartsWithoutExceptionAndSkipsRegistration()
    {
        // Caso "con un valor inválido" de la Acceptance Criteria: un valor
        // presente pero sin la forma "Key=Value;Key2=Value2..." que exige
        // el parser real de Azure.Core. Verificado empíricamente que
        // UseAzureMonitor() NO valida esto de forma síncrona -- el parseo
        // ocurre recién cuando Host.StartAsync arranca el hosted service
        // eager de OpenTelemetry, tumbando el proceso real (reproducido
        // corriendo Api.dll directamente) si se registra igual. Program.cs
        // valida la forma por adelantado y omite el registro en ese caso.
        const string MalformedConnectionString = "this-is-not-a-valid-connection-string";
        Environment.SetEnvironmentVariable(EnvVarName, MalformedConnectionString);
        try
        {
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            Assert.True(response.IsSuccessStatusCode);

            var tracerProvider = factory.Services.GetService<TracerProvider>();
            Assert.Null(tracerProvider);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVarName, null);
        }
    }

    [Fact]
    public async Task Wiring_ConnectionStringWithoutRecognizedKey_StartsWithoutExceptionAndSkipsRegistration()
    {
        // Hallazgo de la segunda ronda de revisión: la forma genérica
        // "Key=Value;Key2=Value2..." no basta -- un valor como "Foo=Bar"
        // (bien formado, pero sin ninguna key reconocida de Application
        // Insights) pasaría el check anterior y llegaría a
        // UseAzureMonitor(), reabriendo la misma excepción eager en
        // Host.StartAsync que el caso "malformado" de arriba. Program.cs
        // exige además que al menos un segmento sea InstrumentationKey= o
        // IngestionEndpoint=.
        const string ConnectionStringWithoutRecognizedKey = "Foo=Bar;Baz=Qux";
        Environment.SetEnvironmentVariable(EnvVarName, ConnectionStringWithoutRecognizedKey);
        try
        {
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            Assert.True(response.IsSuccessStatusCode);

            var tracerProvider = factory.Services.GetService<TracerProvider>();
            Assert.Null(tracerProvider);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVarName, null);
        }
    }
}

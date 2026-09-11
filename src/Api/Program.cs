using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Composition root: Application/Infrastructure services are wired here in later stories.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS: solo los orígenes conocidos, nunca AllowAnyOrigin en el ambiente
// desplegado (spec 1.5). El origen real (SWA) llega vía la variable de
// entorno Cors__AllowedOrigins__0, inyectada por Terraform; localmente
// (ng serve) llega desde appsettings.Development.json.
const string CorsPolicyName = "AppCors";
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Monitoreo básico de disponibilidad (spec 1.8): condicional a
// APPLICATIONINSIGHTS_CONNECTION_STRING (mismo mecanismo estándar de
// configuración, ninguna variable nueva) -- Terraform la inyecta siempre
// en el ambiente desplegado, pero un `dotnet run` local o el build-time
// que corre la app para generar el contrato OpenAPI (spec 1.7) no la
// tienen. Sin el guard, UseAzureMonitor() registraría el hosted service
// igual y Host.StartAsync lanzaría una excepción eager al no encontrar
// connection string, tumbando cualquier arranque sin Application
// Insights desplegado (ver Spec Change Log).
var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    // UseAzureMonitor() NO valida el formato de la connection string de
    // forma síncrona: el parseo real (Azure.Core.ConnectionString.Parse)
    // ocurre de forma diferida cuando Host.StartAsync arranca el hosted
    // service eager de OpenTelemetry -- verificado empíricamente corriendo
    // la app real con un valor mal formado: la excepción atraviesa
    // Host.StartAsync (dentro de app.Run()) y tumba el proceso antes de
    // que Kestrel llegue a escuchar, sin pasar por ningún try/catch que
    // envuelva solo la llamada de registro de abajo. Por eso se valida la
    // forma mínima "Key=Value;Key2=Value2..." (la misma estructura que
    // exige el parser real) por adelantado, para decidir si registrar
    // OpenTelemetry -- un valor con GUID/URL semánticamente incorrectos
    // pero bien formado sí pasa este check (UseAzureMonitor lo acepta sin
    // lanzar; el fallo de conectividad real ocurre después, de forma
    // asíncrona, exportando telemetría en background).
    if (IsWellFormedConnectionString(applicationInsightsConnectionString))
    {
        try
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }
        catch (Exception ex)
        {
            // Defensa adicional por si UseAzureMonitor() llega a lanzar de
            // forma síncrona en algún escenario no cubierto arriba -- se
            // degrada a "sin telemetría" en vez de tumbar el proceso.
            Console.Error.WriteLine($"No se pudo inicializar Application Insights (APPLICATIONINSIGHTS_CONNECTION_STRING inválida): {ex.Message}");
        }
    }
    else
    {
        Console.Error.WriteLine(
            "APPLICATIONINSIGHTS_CONNECTION_STRING vino con formato inválido " +
            "(se esperaba 'Key=Value;Key2=Value2...') -- se continúa sin " +
            "telemetría de Application Insights.");
    }
}

var app = builder.Build();

// Falla fuerte y visible en los logs del Container App si la config CORS
// llegó vacía (ej. Cors__AllowedOrigins__0 no se inyectó bien desde
// Terraform) -- sin esto, el síntoma solo se ve como errores de CORS en la
// consola del navegador de quien lo prueba, nunca en los logs del servidor.
if (corsAllowedOrigins.Length == 0)
{
    app.Logger.LogError(
        "Cors:AllowedOrigins vino vacío -- todo origen será rechazado por CORS. " +
        "Revisar la variable de entorno Cors__AllowedOrigins__0 en el Container App " +
        "(o Cors:AllowedOrigins en appsettings.Development.json en local).");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

// Endpoint real que prueba la cadena código -> CI -> Terraform -> Azure ->
// app corriendo de punta a punta (spec 1.5). Sin auth. Respuesta simple,
// no es un error, así que el envelope RFC 7807 no aplica aquí.
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();

// Validación estructural mínima de una connection string de Application
// Insights ("Key=Value;Key2=Value2..."), sin depender de tipos internos
// del SDK de Azure -- ver comentario junto al guard de OpenTelemetry
// arriba para el porqué (Program.cs). No basta con la forma genérica
// Key=Value: un valor como "Foo=Bar" (bien formado pero sin ninguna key
// reconocida) pasaría el check y llegaría a UseAzureMonitor(), reabriendo
// la misma excepción eager en Host.StartAsync fuera del try/catch (hallazgo
// de la segunda ronda de revisión de spec-1-8) -- por eso también exige al
// menos una de las dos keys que el connection string real de Application
// Insights requiere.
static bool IsWellFormedConnectionString(string candidate)
{
    var segments = candidate.Split(';', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length == 0)
    {
        return false;
    }

    var hasRecognizedKey = false;
    foreach (var segment in segments)
    {
        var separatorIndex = segment.IndexOf('=');
        if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
        {
            return false;
        }

        if (segment.StartsWith("InstrumentationKey=", StringComparison.OrdinalIgnoreCase) ||
            segment.StartsWith("IngestionEndpoint=", StringComparison.OrdinalIgnoreCase))
        {
            hasRecognizedKey = true;
        }
    }

    return hasRecognizedKey;
}

// Expuesto para que tests/Api.Tests use WebApplicationFactory<Program>
// (integration tests -- ver spec 1.5, audit de cobertura CORS).
public partial class Program { }

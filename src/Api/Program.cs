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

// Expuesto para que tests/Api.Tests use WebApplicationFactory<Program>
// (integration tests -- ver spec 1.5, audit de cobertura CORS).
public partial class Program { }

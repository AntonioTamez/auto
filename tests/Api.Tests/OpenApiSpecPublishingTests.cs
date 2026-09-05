using System.Text.Json;

namespace Api.Tests;

/// <summary>
/// Cubre spec-1-7 (publicar el contrato OpenAPI): confirma que el wiring de
/// <c>Api.csproj</c> (<c>OpenApiGenerateDocumentsOnBuild</c> +
/// <c>OpenApiDocumentsDirectory</c>) efectivamente produce un JSON OpenAPI
/// válido durante <c>dotnet build</c>, antes de depender del step
/// <c>upload-artifact</c> de <c>ci.yml</c>. No corre <c>dotnet build</c> por
/// sí misma -- se apoya en que <c>ci.yml</c> (y cualquier `dotnet test`
/// local) siempre corre después de un `dotnet build` sobre la misma
/// solución, momento en el que este archivo ya debe existir.
/// </summary>
public class OpenApiSpecPublishingTests
{
    // Ruta fija por Api.csproj (OpenApiDocumentsDirectory =
    // $(BaseIntermediateOutputPath)openapi\) -- ver spec-1-7 Code Map.
    // Deliberadamente NO en la raíz del proyecto (ver Spec Change Log de
    // spec-1-7): esa ruta se filtraría a `dotnet publish`.
    private const string RelativeOpenApiJsonPath = "src/Api/obj/openapi/Api.json";

    [Fact]
    public void Build_GeneratesOpenApiJson_AtExpectedPath()
    {
        var path = ResolveOpenApiJsonPath();

        Assert.True(
            File.Exists(path),
            $"No se encontró '{RelativeOpenApiJsonPath}' en '{path}'. " +
            "Esperado: 'dotnet build' sobre Api.csproj genera este archivo " +
            "(OpenApiGenerateDocumentsOnBuild=true) antes de que corran los tests.");
    }

    [Fact]
    public void Build_GeneratedOpenApiJson_IsValidSpecWithHealthPath()
    {
        var path = ResolveOpenApiJsonPath();
        Assert.True(File.Exists(path), $"No se encontró '{RelativeOpenApiJsonPath}' en '{path}'.");

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("openapi", out var openApiVersion), "El JSON generado debe traer la propiedad 'openapi'.");
        Assert.StartsWith("3.", openApiVersion.GetString());

        Assert.True(root.TryGetProperty("paths", out var paths), "El JSON generado debe traer la propiedad 'paths'.");
        Assert.True(paths.TryGetProperty("/health", out _), "El contrato generado debe describir el endpoint '/health' (spec 1.5).");
    }

    /// <summary>
    /// El test corre desde el directorio de salida del build de
    /// Api.Tests.csproj (tests/Api.Tests/bin/&lt;config&gt;/&lt;tfm&gt;), no
    /// desde la raíz del repo -- sube por el árbol de directorios hasta
    /// encontrar Auto.slnx para anclar la ruta relativa del JSON generado
    /// por Api.csproj de forma robusta frente a Debug/Release y al TFM.
    /// </summary>
    private static string ResolveOpenApiJsonPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Auto.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                $"No se pudo ubicar la raíz del repo (Auto.slnx) subiendo desde '{AppContext.BaseDirectory}'.");
        }

        return Path.Combine(directory.FullName, RelativeOpenApiJsonPath.Replace('/', Path.DirectorySeparatorChar));
    }
}

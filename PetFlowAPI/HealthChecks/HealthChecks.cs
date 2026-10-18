using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PetFlowAPI.HealthChecks;

/// <summary>Verifica a disponibilidade de serviços HTTP externos configurados em HealthChecks:ExternalServices.</summary>
public sealed class ExternalServicesHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ExternalServicesHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var urls = _configuration.GetSection("HealthChecks:ExternalServices").Get<string[]>() ?? Array.Empty<string>();
        if (urls.Length == 0) return HealthCheckResult.Healthy("Nenhum serviço externo configurado.");

        var client = _httpClientFactory.CreateClient("health-check");
        var failures = new List<string>();
        foreach (var url in urls)
        {
            try
            {
                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) failures.Add($"{url}: {(int)response.StatusCode}");
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                failures.Add($"{url}: indisponível");
            }
        }

        return failures.Count == 0
            ? HealthCheckResult.Healthy("Serviços externos disponíveis.")
            : HealthCheckResult.Unhealthy("Um ou mais serviços externos estão indisponíveis.", data: new Dictionary<string, object> { ["falhas"] = failures });
    }
}

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, Options));
    }
}

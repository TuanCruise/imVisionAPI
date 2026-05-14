using ImVision.Integration.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ImVision.Integration.HealthChecks;

public class ImVisionHealthCheck : IHealthCheck
{
    private readonly ImVisionApiClient _apiClient;
    private readonly ILogger<ImVisionHealthCheck> _logger;

    public ImVisionHealthCheck(ImVisionApiClient apiClient, ILogger<ImVisionHealthCheck> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Pinging the Alarms endpoint (get all)
            var endpoint = ImVisionEndpointBuilder.BuildGetAlarmsEndpoint(null, null, null);
            await _apiClient.GetAsync<object>(endpoint, cancellationToken);
            return HealthCheckResult.Healthy("imVision API is reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "imVision API health check failed.");
            return HealthCheckResult.Unhealthy("imVision API is unreachable.", ex);
        }
    }
}

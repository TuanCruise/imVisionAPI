using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImVision.Integration.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImVision.Integration.Http;

public class ImVisionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImVisionApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ImVisionApiClient(HttpClient httpClient, ILogger<ImVisionApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, endpoint);
        _logger.LogInformation("Sending GET request to {RequestUrl}", requestUrl.PathAndQuery);
        
        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;
        
        try
        {
            response = await _httpClient.GetAsync(endpoint, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogError(ex, "Request to {RequestUrl} timed out after {Duration}ms.", requestUrl.PathAndQuery, sw.ElapsedMilliseconds);
            throw new ImVisionException($"Request to {endpoint} timed out.", ex, 408);
        }

        sw.Stop();
        _logger.LogInformation("Received {StatusCode} from {RequestUrl} in {Duration}ms", (int)response.StatusCode, requestUrl.PathAndQuery, sw.ElapsedMilliseconds);

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return default;
                
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        }

        await HandleErrorResponseAsync(response, requestUrl.PathAndQuery, cancellationToken);
        return default;
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response, string endpoint, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        _logger.LogWarning("Error response from {Endpoint}: {StatusCode} - {Content}", endpoint, (int)response.StatusCode, content);

        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.BadRequest:
                throw new ImVisionBadRequestException($"Bad Request to imVision API: {content}");
            case System.Net.HttpStatusCode.Unauthorized:
            case System.Net.HttpStatusCode.Forbidden:
                throw new ImVisionUnauthorizedException("Unauthorized access to imVision API.");
            case System.Net.HttpStatusCode.NotFound:
                throw new ImVisionNotFoundException($"Resource not found in imVision API: {endpoint}");
            case System.Net.HttpStatusCode.InternalServerError:
            case System.Net.HttpStatusCode.ServiceUnavailable:
                throw new ImVisionServerException($"imVision API Server Error: {content}", (int)response.StatusCode);
            default:
                throw new ImVisionException($"imVision API Error: {content}", (int)response.StatusCode);
        }
    }
}

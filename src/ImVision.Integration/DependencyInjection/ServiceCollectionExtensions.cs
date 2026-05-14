using ImVision.Integration.HealthChecks;
using ImVision.Integration.Http;
using ImVision.Integration.Options;
using ImVision.Integration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace ImVision.Integration.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImVisionIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ImVisionOptions>(configuration.GetSection(ImVisionOptions.SectionName));

        // Validate early
        var options = configuration.GetSection(ImVisionOptions.SectionName).Get<ImVisionOptions>();
        if (string.IsNullOrWhiteSpace(options?.BaseUrl))
        {
            throw new ArgumentException("ImVision BaseUrl is missing in configuration.");
        }

        services.AddTransient<ImVisionAuthHandler>();

        services.AddHttpClient<ImVisionApiClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<ImVisionOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds > 0 ? opt.TimeoutSeconds : 30);
        })
        .AddHttpMessageHandler<ImVisionAuthHandler>()
        .AddPolicyHandler((sp, request) =>
        {
            var opt = sp.GetRequiredService<IOptions<ImVisionOptions>>().Value;
            var retryCount = opt.RetryCount > 0 ? opt.RetryCount : 3;
            var baseDelay = opt.RetryBaseDelayMs > 0 ? opt.RetryBaseDelayMs : 500;

            // Polly Retry: Handles 500, 503 and 408 (Timeout). 
            return HttpPolicyExtensions
                .HandleTransientHttpError() 
                .WaitAndRetryAsync(retryCount, retryAttempt => 
                    TimeSpan.FromMilliseconds(baseDelay * Math.Pow(2, retryAttempt - 1)));
        });

        services.AddScoped<IImVisionAlarmService, ImVisionAlarmService>();

        services.AddHealthChecks()
            .AddCheck<ImVisionHealthCheck>("ImVision_API_Check", tags: new[] { "imvision", "integration" });

        return services;
    }
}

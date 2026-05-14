using ImVision.Integration.Services;

namespace ImVision.Host;

public class ImVisionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImVisionWorker> _logger;

    public ImVisionWorker(IServiceProvider serviceProvider, ILogger<ImVisionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImVisionWorker starting at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Vì ImVisionAlarmService được đăng ký dạng Scoped, ta cần tạo scope mới
                using var scope = _serviceProvider.CreateScope();
                var alarmService = scope.ServiceProvider.GetRequiredService<IImVisionAlarmService>();

                _logger.LogInformation("==================================================");
                _logger.LogInformation("TESTING: Fetching Alarms from imVision API...");

                // Ví dụ: Lấy danh sách Alarm của ngày hôm nay và enrich Event
                var startTime = DateTimeOffset.UtcNow.AddDays(-1);
                
                var alarmEvents = await alarmService.GetAlarmEventsAsync(
                    alarmType: 1, 
                    startTime: startTime, 
                    endTime: null, 
                    enrichEventDetail: true, 
                    cancellationToken: stoppingToken);

                if (alarmEvents.Any())
                {
                    foreach (var item in alarmEvents.Take(5)) // In ra tối đa 5 alarm để test
                    {
                        _logger.LogInformation(">> Found Alarm: {AlarmName} (ID: {AlarmId}) - Attached Event: {EventName}", 
                            item.Alarm.Name, item.Alarm.Id, item.Event?.Name ?? "NONE");
                    }
                }
                else
                {
                    _logger.LogInformation(">> No alarms found in the specified time range.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling imVision API.");
            }

            _logger.LogInformation("Sleeping for 10 seconds before next poll...");
            _logger.LogInformation("==================================================");
            
            await Task.Delay(10000, stoppingToken);
        }
    }
}

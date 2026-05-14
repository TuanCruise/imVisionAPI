using ImVision.Integration.Dtos;
using ImVision.Integration.Exceptions;
using ImVision.Integration.Http;
using Microsoft.Extensions.Logging;

namespace ImVision.Integration.Services;

public class ImVisionAlarmService : IImVisionAlarmService
{
    private readonly ImVisionApiClient _apiClient;
    private readonly ILogger<ImVisionAlarmService> _logger;

    public ImVisionAlarmService(ImVisionApiClient apiClient, ILogger<ImVisionAlarmService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ImVisionAlarmDto>> GetAllAlarmsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to fetch all alarms from imVision.");
        var endpoint = ImVisionEndpointBuilder.BuildGetAlarmsEndpoint(null, null, null);
        var alarms = await _apiClient.GetAsync<List<ImVisionAlarmDto>>(endpoint, cancellationToken);
        var result = alarms ?? new List<ImVisionAlarmDto>();
        _logger.LogInformation("Successfully fetched {Count} alarms from imVision.", result.Count);
        return result;
    }

    public async Task<IReadOnlyList<ImVisionAlarmDto>> GetAlarmsAsync(
        int alarmType,
        DateTimeOffset startTime,
        DateTimeOffset? endTime,
        CancellationToken cancellationToken = default)
    {
        if (alarmType <= 0)
        {
            _logger.LogWarning("Invalid AlarmType requested: {AlarmType}", alarmType);
            throw new ArgumentException("AlarmType must be greater than 0.", nameof(alarmType));
        }

        if (endTime.HasValue && startTime > endTime.Value)
        {
            _logger.LogWarning("Invalid time range. StartTime: {StartTime}, EndTime: {EndTime}", startTime, endTime);
            throw new ArgumentException("StartTime cannot be greater than EndTime.");
        }

        _logger.LogInformation("Fetching alarms with Type: {AlarmType}, StartTime: {StartTime}, EndTime: {EndTime}", alarmType, startTime, endTime);
        var endpoint = ImVisionEndpointBuilder.BuildGetAlarmsEndpoint(alarmType, startTime, endTime);
        var alarms = await _apiClient.GetAsync<List<ImVisionAlarmDto>>(endpoint, cancellationToken);
        var result = alarms ?? new List<ImVisionAlarmDto>();
        _logger.LogInformation("Successfully fetched {Count} alarms for Type: {AlarmType}.", result.Count, alarmType);
        return result;
    }

    public async Task<ImVisionAlarmDto?> GetAlarmByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching alarm by Id: {Id}", id);
            var endpoint = ImVisionEndpointBuilder.BuildGetAlarmByIdEndpoint(id);
            var result = await _apiClient.GetAsync<ImVisionAlarmDto>(endpoint, cancellationToken);
            _logger.LogInformation("Successfully fetched alarm by Id: {Id}", id);
            return result;
        }
        catch (ImVisionNotFoundException)
        {
            _logger.LogDebug("Alarm with Id: {Id} was not found (404 ignored).", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<ImVisionEventDto>> GetEventsAsync(
        int? eventType,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime,
        CancellationToken cancellationToken = default)
    {
        if (endTime.HasValue && startTime.HasValue && startTime.Value > endTime.Value)
        {
            _logger.LogWarning("Invalid time range. StartTime: {StartTime}, EndTime: {EndTime}", startTime, endTime);
            throw new ArgumentException("StartTime cannot be greater than EndTime.");
        }

        _logger.LogInformation("Fetching events with Type: {EventType}, StartTime: {StartTime}, EndTime: {EndTime}", eventType, startTime, endTime);
        var endpoint = ImVisionEndpointBuilder.BuildGetEventsEndpoint(eventType, startTime, endTime);
        var events = await _apiClient.GetAsync<List<ImVisionEventDto>>(endpoint, cancellationToken);
        var result = events ?? new List<ImVisionEventDto>();
        _logger.LogInformation("Successfully fetched {Count} events.", result.Count);
        return result;
    }

    public async Task<ImVisionEventDto?> GetEventByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching event by Id: {Id}", id);
            var endpoint = ImVisionEndpointBuilder.BuildGetEventByIdEndpoint(id);
            var result = await _apiClient.GetAsync<ImVisionEventDto>(endpoint, cancellationToken);
            _logger.LogInformation("Successfully fetched event by Id: {Id}", id);
            return result;
        }
        catch (ImVisionNotFoundException)
        {
            _logger.LogDebug("Event with Id: {Id} was not found (404 ignored).", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<ImVisionAlarmEventDto>> GetAlarmEventsAsync(
        int alarmType,
        DateTimeOffset startTime,
        DateTimeOffset? endTime,
        bool enrichEventDetail,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting GetAlarmEventsAsync with AlarmType: {AlarmType}, EnrichEventDetail: {EnrichEventDetail}", alarmType, enrichEventDetail);
        var alarms = await GetAlarmsAsync(alarmType, startTime, endTime, cancellationToken);
        if (!alarms.Any())
        {
            _logger.LogInformation("No alarms found to enrich.");
            return new List<ImVisionAlarmEventDto>();
        }

        var results = new List<ImVisionAlarmEventDto>();
        // Caching tránh gọi API cho EventId bị trùng lặp
        var eventCache = new Dictionary<int, ImVisionEventDto?>();
        int enrichedCount = 0;

        foreach (var alarm in alarms)
        {
            var dto = new ImVisionAlarmEventDto { Alarm = alarm };

            if (enrichEventDetail && alarm.EventId > 0)
            {
                if (!eventCache.TryGetValue(alarm.EventId, out var eventDto))
                {
                    try
                    {
                        eventDto = await GetEventByIdAsync(alarm.EventId, cancellationToken);
                        eventCache[alarm.EventId] = eventDto;
                        if (eventDto != null) enrichedCount++;
                    }
                    catch (Exception ex) when (ex is not TaskCanceledException)
                    {
                        _logger.LogWarning(ex, "Failed to enrich Event details for EventId {EventId} attached to AlarmId {AlarmId}", alarm.EventId, alarm.Id);
                        eventCache[alarm.EventId] = null;
                        eventDto = null;
                    }
                }
                dto.Event = eventDto;
            }
            results.Add(dto);
        }

        _logger.LogInformation("Completed GetAlarmEventsAsync. Total Alarms: {AlarmCount}, Unique Events Enriched: {EnrichedCount}", results.Count, enrichedCount);
        return results;
    }
}

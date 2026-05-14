using ImVision.Integration.Constants;

namespace ImVision.Integration.Http;

public static class ImVisionEndpointBuilder
{
    public static string BuildGetAlarmsEndpoint(int? alarmType, DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        var queryParams = new List<string>();

        if (alarmType.HasValue)
            queryParams.Add($"alarmType={alarmType.Value}");
        if (startTime.HasValue)
            queryParams.Add($"startTime={Uri.EscapeDataString(startTime.Value.ToString("O"))}");
        if (endTime.HasValue)
            queryParams.Add($"endTime={Uri.EscapeDataString(endTime.Value.ToString("O"))}");

        if (queryParams.Any())
            return $"{ImVisionEndpoints.Alarms}?{string.Join("&", queryParams)}";

        return ImVisionEndpoints.Alarms;
    }

    public static string BuildGetAlarmByIdEndpoint(int id)
    {
        return $"{ImVisionEndpoints.Alarms}/{id}";
    }

    public static string BuildGetEventsEndpoint(int? eventType, DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        var queryParams = new List<string>();

        if (eventType.HasValue)
            queryParams.Add($"eventType={eventType.Value}");
        if (startTime.HasValue)
            queryParams.Add($"startTime={Uri.EscapeDataString(startTime.Value.ToString("O"))}");
        if (endTime.HasValue)
            queryParams.Add($"endTime={Uri.EscapeDataString(endTime.Value.ToString("O"))}");

        if (queryParams.Any())
            return $"{ImVisionEndpoints.Events}?{string.Join("&", queryParams)}";

        return ImVisionEndpoints.Events;
    }

    public static string BuildGetEventByIdEndpoint(int id)
    {
        return $"{ImVisionEndpoints.Events}/{id}";
    }
}

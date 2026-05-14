using ImVision.Integration.Dtos;

namespace ImVision.Integration.Services;

public interface IImVisionAlarmService
{
    Task<IReadOnlyList<ImVisionAlarmDto>> GetAllAlarmsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImVisionAlarmDto>> GetAlarmsAsync(
        int alarmType,
        DateTimeOffset startTime,
        DateTimeOffset? endTime,
        CancellationToken cancellationToken = default);

    Task<ImVisionAlarmDto?> GetAlarmByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImVisionEventDto>> GetEventsAsync(
        int? eventType,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime,
        CancellationToken cancellationToken = default);

    Task<ImVisionEventDto?> GetEventByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImVisionAlarmEventDto>> GetAlarmEventsAsync(
        int alarmType,
        DateTimeOffset startTime,
        DateTimeOffset? endTime,
        bool enrichEventDetail,
        CancellationToken cancellationToken = default);
}

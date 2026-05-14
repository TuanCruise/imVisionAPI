namespace ImVision.Integration.Dtos;

public class ImVisionAlarmEventDto
{
    public ImVisionAlarmDto Alarm { get; set; } = null!;
    public ImVisionEventDto? Event { get; set; }
}

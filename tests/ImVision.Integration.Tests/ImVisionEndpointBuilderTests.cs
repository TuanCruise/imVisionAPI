using ImVision.Integration.Http;
using Xunit;
using FluentAssertions;

namespace ImVision.Integration.Tests;

public class ImVisionEndpointBuilderTests
{
    [Fact]
    public void BuildGetAlarmsEndpoint_WithoutParameters_ReturnsBaseAlarmsEndpoint()
    {
        var result = ImVisionEndpointBuilder.BuildGetAlarmsEndpoint(null, null, null);
        result.Should().Be("api/alarms");
    }

    [Fact]
    public void BuildGetAlarmByIdEndpoint_ReturnsCorrectEndpoint()
    {
        var result = ImVisionEndpointBuilder.BuildGetAlarmByIdEndpoint(123);
        result.Should().Be("api/alarms/123");
    }

    [Fact]
    public void BuildGetAlarmsEndpoint_WithParameters_ReturnsCorrectlyFormattedAndEncodedUrl()
    {
        var startTime = new DateTimeOffset(2023, 10, 1, 10, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2023, 10, 2, 10, 0, 0, TimeSpan.Zero);
        
        var result = ImVisionEndpointBuilder.BuildGetAlarmsEndpoint(5, startTime, endTime);
        
        result.Should().Contain("alarmType=5");
        result.Should().Contain($"startTime={Uri.EscapeDataString(startTime.ToString("O"))}");
        result.Should().Contain($"endTime={Uri.EscapeDataString(endTime.ToString("O"))}");
    }

    [Fact]
    public void BuildGetEventsEndpoint_WithParameters_ReturnsCorrectlyFormattedUrl()
    {
        var startTime = new DateTimeOffset(2023, 10, 1, 10, 0, 0, TimeSpan.Zero);
        var result = ImVisionEndpointBuilder.BuildGetEventsEndpoint(10, startTime, null);
        
        result.Should().Contain("eventType=10");
        result.Should().Contain($"startTime={Uri.EscapeDataString(startTime.ToString("O"))}");
        result.Should().NotContain("endTime=");
    }
}

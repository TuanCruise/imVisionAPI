using ImVision.Integration.Dtos;
using ImVision.Integration.Http;
using ImVision.Integration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;
using FluentAssertions;

namespace ImVision.Integration.Tests;

public class ImVisionAlarmServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly ImVisionAlarmService _service;

    public ImVisionAlarmServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_handlerMock.Object) { BaseAddress = new Uri("https://test.com") };
        var apiClient = new ImVisionApiClient(httpClient, NullLogger<ImVisionApiClient>.Instance);
        _service = new ImVisionAlarmService(apiClient, NullLogger<ImVisionAlarmService>.Instance);
    }

    [Fact]
    public async Task GetAlarmsAsync_ValidatesInputs()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAlarmsAsync(0, DateTimeOffset.UtcNow, null));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAlarmsAsync(1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task GetAlarmByIdAsync_ReturnsNull_WhenNotFound()
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") });

        var result = await _service.GetAlarmByIdAsync(1);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAlarmEventsAsync_EnrichesEvent_CorrectlyAndCaches()
    {
        var alarmResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("[{\"id\": 1, \"eventId\": 100}, {\"id\": 2, \"eventId\": 100}]")
        };

        var eventResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"id\": 100, \"name\": \"Event100\"}")
        };

        _handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(alarmResponse)
            .ReturnsAsync(eventResponse); 

        var results = await _service.GetAlarmEventsAsync(1, DateTimeOffset.UtcNow, null, enrichEventDetail: true);

        results.Should().HaveCount(2);
        results[0].Event.Should().NotBeNull();
        results[0].Event!.Id.Should().Be(100);
        results[1].Event.Should().NotBeNull();
        
        _handlerMock.Protected().Verify(
            "SendAsync", 
            Times.Exactly(2), 
            ItExpr.IsAny<HttpRequestMessage>(), 
            ItExpr.IsAny<CancellationToken>());
    }
}

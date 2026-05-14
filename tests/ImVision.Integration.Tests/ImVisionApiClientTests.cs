using System.Net;
using ImVision.Integration.Exceptions;
using ImVision.Integration.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;

namespace ImVision.Integration.Tests;

public class ImVisionApiClientTests
{
    [Fact]
    public async Task GetAsync_ThrowsImVisionNotFoundException_On404()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://test.com") };
        var apiClient = new ImVisionApiClient(httpClient, NullLogger<ImVisionApiClient>.Instance);

        await Assert.ThrowsAsync<ImVisionNotFoundException>(() => apiClient.GetAsync<object>("api/test"));
    }

    [Fact]
    public async Task GetAsync_ThrowsImVisionBadRequestException_On400()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Params") });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://test.com") };
        var apiClient = new ImVisionApiClient(httpClient, NullLogger<ImVisionApiClient>.Instance);

        var ex = await Assert.ThrowsAsync<ImVisionBadRequestException>(() => apiClient.GetAsync<object>("api/test"));
        ex.Message.Should().Contain("Bad Params");
    }

    [Fact]
    public async Task GetAsync_ThrowsImVisionUnauthorizedException_On401()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("") });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://test.com") };
        var apiClient = new ImVisionApiClient(httpClient, NullLogger<ImVisionApiClient>.Instance);

        await Assert.ThrowsAsync<ImVisionUnauthorizedException>(() => apiClient.GetAsync<object>("api/test"));
    }
}

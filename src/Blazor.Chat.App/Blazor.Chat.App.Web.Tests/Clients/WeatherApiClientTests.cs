using Blazor.Chat.App.Web;
using FluentAssertions;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace Blazor.Chat.App.Web.Tests.Clients;

/// <summary>
/// Unit tests for the WeatherApiClient class.
/// Tests HTTP client interactions for weather forecast retrieval.
/// </summary>
[TestFixture]
public class WeatherApiClientTests
{
    private MockHttpMessageHandler _mockHttp = null!;
    private HttpClient _httpClient = null!;
    private WeatherApiClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost");
        _client = new WeatherApiClient(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
    }

    [Test]
    public async Task GetWeatherAsync_WithDefaultMaxItems_ReturnsTenForecasts()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>();
        for (int i = 0; i < 10; i++)
        {
            forecasts.Add(new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                20 + i,
                $"Weather {i}"));
        }

        var jsonResponse = $"[{string.Join(",", forecasts.Select(f => JsonSerializer.Serialize(f)))}]";
        
        _mockHttp.When(HttpMethod.Get, "/weatherforecast")
            .Respond("application/json", jsonResponse);

        // Act
        var result = await _client.GetWeatherAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
    }

    [Test]
    public async Task GetWeatherAsync_WithCustomMaxItems_ReturnsSpecifiedNumber()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>();
        for (int i = 0; i < 20; i++)
        {
            forecasts.Add(new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                20 + i,
                $"Weather {i}"));
        }

        var jsonResponse = $"[{string.Join(",", forecasts.Select(f => JsonSerializer.Serialize(f)))}]";
        
        _mockHttp.When(HttpMethod.Get, "/weatherforecast")
            .Respond("application/json", jsonResponse);

        // Act
        var result = await _client.GetWeatherAsync(maxItems: 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    [Test]
    public async Task GetWeatherAsync_WhenServerReturnsEmpty_ReturnsEmptyArray()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Get, "/weatherforecast")
            .Respond("application/json", "[]");

        // Act
        var result = await _client.GetWeatherAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetWeatherAsync_WithSingleItem_ReturnsOneItem()
    {
        // Arrange
        var forecast = new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now),
            25,
            "Sunny");

        var jsonResponse = $"[{JsonSerializer.Serialize(forecast)}]";
        
        _mockHttp.When(HttpMethod.Get, "/weatherforecast")
            .Respond("application/json", jsonResponse);

        // Act
        var result = await _client.GetWeatherAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TemperatureC.Should().Be(25);
        result[0].Summary.Should().Be("Sunny");
    }

    [Test]
    public async Task GetWeatherAsync_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Get, "/weatherforecast")
            .Respond(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        Func<Task> act = async () => await _client.GetWeatherAsync(cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Test]
    public async Task GetWeatherAsync_WithMoreItemsThanMaxItems_StopsAtMaxItems()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>();
        for (int i = 0; i < 100; i++)
        {
            forecasts.Add(new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                20 + i,
                $"Weather {i}"));
        }

        var jsonResponse = $"[{string.Join(",", forecasts.Select(f => JsonSerializer.Serialize(f)))}]";
        
        _mockHttp.When(HttpMethod.Get, "/weatherforecast")
            .Respond("application/json", jsonResponse);

        // Act
        var result = await _client.GetWeatherAsync(maxItems: 3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }
}

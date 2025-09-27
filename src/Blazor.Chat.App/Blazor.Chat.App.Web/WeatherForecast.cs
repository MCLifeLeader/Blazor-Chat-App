namespace Blazor.Chat.App.Web
{
    public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC * 9.0 / 5.0);
    }
}
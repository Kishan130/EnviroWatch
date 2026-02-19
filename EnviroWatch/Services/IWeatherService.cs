using EnviroWatch.Models;

namespace EnviroWatch.Services
{
    public interface IWeatherService
    {
        Task<WeatherSnapshot?> GetCurrentWeatherAsync(int districtId);
        Task<List<WeatherSnapshot>> GetHistoricalWeatherAsync(
            int districtId, int days);
    }
}

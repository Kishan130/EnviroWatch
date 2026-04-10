using EnviroWatch.Models;

namespace EnviroWatch.Services
{
    public interface IWeatherService
    {
        Task<WeatherSnapshot?> GetCurrentWeatherAsync(District district);
        Task<List<WeatherSnapshot>> GetHistoricalAsync(int districtId, int days);
    }
}

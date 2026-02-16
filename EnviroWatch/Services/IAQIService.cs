using EnviroWatch.Models;

namespace EnviroWatch.Services
{
    public interface IAQIService
    {
        Task<AQISnapshot?> GetCurrentAQIAsync(int districtId);
        Task<List<AQISnapshot>> GetHistoricalAQIAsync(int districtId, int days);
        string GetAQICategory(int aqi);
        string GetAQIColor(int aqi);
    }
}

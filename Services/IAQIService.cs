using EnviroWatch.Models;

namespace EnviroWatch.Services
{
    public interface IAQIService
    {
        Task<AQISnapshot?> GetCurrentAQIAsync(District district);
        Task<List<AQISnapshot>> GetHistoricalAsync(int districtId, int days);
        Task<List<AQISnapshot>> GetHistoricalFromApiAsync(District district, DateTime start, DateTime end);
    }
}

using EnviroWatch.Models;

namespace EnviroWatch.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportToCsvAsync(int districtId, int days);
        Task<byte[]> ExportToPdfAsync(int districtId, int days);
    }
}

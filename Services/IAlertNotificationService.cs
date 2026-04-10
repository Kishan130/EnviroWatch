namespace EnviroWatch.Services
{
    public interface IAlertNotificationService
    {
        Task SendEmailAlertAsync(string email, string cityName, int aqi, string category, int threshold);
        Task SendSmsAlertAsync(string phoneNumber, string cityName, int aqi, string category, int threshold);
    }
}

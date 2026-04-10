using System.Net;
using System.Net.Mail;

namespace EnviroWatch.Services
{
    public class AlertNotificationService : IAlertNotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AlertNotificationService> _logger;

        public AlertNotificationService(IConfiguration config, ILogger<AlertNotificationService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAlertAsync(string email, string cityName, int aqi, string category, int threshold)
        {
            try
            {
                var smtpHost = _config["Smtp:Host"];
                var smtpPort = _config.GetValue("Smtp:Port", 587);
                var smtpUser = _config["Smtp:Username"];
                var smtpPass = _config["Smtp:Password"];
                var fromEmail = _config["Smtp:FromEmail"] ?? "alerts@envirowatch.in";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogWarning("SMTP not configured. Email alert for {City} (AQI={AQI}) to {Email} logged but NOT sent.", cityName, aqi, email);
                    _logger.LogInformation("[EMAIL ALERT] To: {Email} | {City}: AQI {AQI} ({Category}) exceeds threshold {Threshold}", email, cityName, aqi, category, threshold);
                    return;
                }

                var subject = $"⚠️ AQI Alert: {cityName} AQI is {aqi} ({category})";
                var body = $@"
<html>
<body style='font-family: Arial, sans-serif; background: #0a0f1c; color: #f1f5f9; padding: 24px;'>
    <div style='max-width: 500px; margin: 0 auto; background: rgba(17,24,39,0.95); border-radius: 12px; padding: 24px; border: 1px solid rgba(255,255,255,0.1);'>
        <h2 style='color: #22d3ee; margin-bottom: 8px;'>🌿 EnviroWatch Alert</h2>
        <p style='color: #94a3b8;'>Your AQI threshold has been exceeded.</p>
        <hr style='border-color: rgba(255,255,255,0.1);' />
        <table style='width: 100%; color: #f1f5f9;'>
            <tr><td style='padding: 8px;'><strong>City</strong></td><td style='padding: 8px;'>{cityName}</td></tr>
            <tr><td style='padding: 8px;'><strong>Current AQI</strong></td><td style='padding: 8px; color: #ef4444; font-weight: 700;'>{aqi}</td></tr>
            <tr><td style='padding: 8px;'><strong>Category</strong></td><td style='padding: 8px;'>{category}</td></tr>
            <tr><td style='padding: 8px;'><strong>Your Threshold</strong></td><td style='padding: 8px;'>{threshold}</td></tr>
        </table>
        <p style='color: #64748b; font-size: 12px; margin-top: 16px;'>— EnviroWatch | India AQI Monitor</p>
    </div>
</body>
</html>";

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var message = new MailMessage(fromEmail, email, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email alert sent to {Email} for {City} (AQI={AQI})", email, cityName, aqi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email alert to {Email}", email);
            }
        }

        public async Task SendSmsAlertAsync(string phoneNumber, string cityName, int aqi, string category, int threshold)
        {
            // Placeholder: SMS sending requires Twilio or similar service
            // This logs the alert to console. To enable real SMS:
            // 1. Install Twilio NuGet package
            // 2. Configure Twilio:AccountSid, Twilio:AuthToken, Twilio:FromNumber in appsettings.json
            // 3. Replace this method body with Twilio API calls

            _logger.LogInformation("[SMS ALERT] To: {Phone} | {City}: AQI {AQI} ({Category}) exceeds threshold {Threshold}",
                phoneNumber, cityName, aqi, category, threshold);

            await Task.CompletedTask;
        }
    }
}

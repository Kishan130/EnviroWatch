using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Services
{
    public class DataSnapshotService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataSnapshotService> _logger;
        private readonly TimeSpan _interval;
        private const int BatchSize = 50;

        public DataSnapshotService(IServiceProvider serviceProvider, IConfiguration config, ILogger<DataSnapshotService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(config.GetValue("SnapshotSettings:IntervalMinutes", 60));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataSnapshotService started. Interval: {Interval} minutes", _interval.TotalMinutes);

            // Initial delay to let the app start up
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CaptureSnapshotsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error capturing snapshots");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CaptureSnapshotsAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();
            var aqiService = scope.ServiceProvider.GetRequiredService<IAQIService>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertNotificationService>();

            var districts = await db.Districts.ToListAsync(ct);
            _logger.LogInformation("Capturing weather snapshots for {Count} districts...", districts.Count);

            int totalBatches = (int)Math.Ceiling((double)districts.Count / BatchSize);
            int successCount = 0;

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                if (ct.IsCancellationRequested) break;

                var batch = districts.Skip(batchIndex * BatchSize).Take(BatchSize).ToList();

                foreach (var district in batch)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        var weather = await weatherService.GetCurrentWeatherAsync(district);
                        if (weather != null)
                        {
                            db.WeatherSnapshots.Add(weather);
                            successCount++;
                        }

                        // Also capture AQI snapshot
                        var aqi = await aqiService.GetCurrentAQIAsync(district);
                        if (aqi != null)
                        {
                            db.AQISnapshots.Add(aqi);

                            // Check against user alert thresholds
                            await CheckAndSendAlertsAsync(db, alertService, district, aqi, ct);
                        }

                        // Small delay to respect rate limits (free tier: 60 calls/min)
                        await Task.Delay(500, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed snapshot for {City}", district.Name);
                    }
                }

                // Save after each batch to avoid losing data on failure
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Batch {Current}/{Total} complete. {Success} snapshots captured so far.",
                    batchIndex + 1, totalBatches, successCount);

                // Pause between batches
                if (batchIndex < totalBatches - 1)
                {
                    await Task.Delay(2000, ct);
                }
            }

            _logger.LogInformation("Snapshot capture complete. {Success}/{Total} districts captured.",
                successCount, districts.Count);
        }

        private async Task CheckAndSendAlertsAsync(AppDbContext db, IAlertNotificationService alertService,
            District district, AQISnapshot aqi, CancellationToken ct)
        {
            try
            {
                // Find all active subscriptions for this district where AQI exceeds threshold
                var matchedSubs = await db.UserSubscriptions
                    .Where(s => s.DistrictId == district.Id && s.IsActive && s.AQIThreshold <= aqi.AQI)
                    .ToListAsync(ct);

                foreach (var sub in matchedSubs)
                {
                    if (sub.NotifyEmail && !string.IsNullOrEmpty(sub.Email))
                    {
                        await alertService.SendEmailAlertAsync(sub.Email, district.Name, aqi.AQI, aqi.Category, sub.AQIThreshold);
                    }

                    if (sub.NotifySMS && !string.IsNullOrEmpty(sub.PhoneNumber))
                    {
                        await alertService.SendSmsAlertAsync(sub.PhoneNumber, district.Name, aqi.AQI, aqi.Category, sub.AQIThreshold);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check/send alerts for {City}", district.Name);
            }
        }
    }
}

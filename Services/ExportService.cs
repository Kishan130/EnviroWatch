using System.Text;
using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnviroWatch.Services
{
    public class ExportService : IExportService
    {
        private readonly AppDbContext _db;
        private readonly IAQIService _aqiService;
        private readonly IWeatherService _weatherService;

        public ExportService(AppDbContext db, IAQIService aqiService, IWeatherService weatherService)
        {
            _db = db;
            _aqiService = aqiService;
            _weatherService = weatherService;
        }

        public async Task<byte[]> ExportToCsvAsync(int districtId, int days)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var district = await _db.Districts.FindAsync(districtId);
            var weatherData = await _db.WeatherSnapshots
                .Where(w => w.DistrictId == districtId && w.RecordedAt >= since)
                .OrderByDescending(w => w.RecordedAt)
                .ToListAsync();
            var aqiData = await _db.AQISnapshots
                .Where(a => a.DistrictId == districtId && a.RecordedAt >= since)
                .OrderByDescending(a => a.RecordedAt)
                .ToListAsync();

            // Fix 8: If no AQI data from DB, fetch from API
            if (aqiData.Count == 0 && district != null)
            {
                var start = DateTime.UtcNow.AddDays(-days);
                var end = DateTime.UtcNow;
                var apiData = await _aqiService.GetHistoricalFromApiAsync(district, start, end);
                aqiData = apiData.OrderByDescending(a => a.RecordedAt).ToList();
            }

            // Fix 8: If no weather data from DB, fetch current weather
            WeatherSnapshot? currentWeather = null;
            if (weatherData.Count == 0 && district != null)
            {
                currentWeather = await _weatherService.GetCurrentWeatherAsync(district);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"EnviroWatch Report - {district?.Name ?? "Unknown"} - Last {days} Days");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Weather data
            sb.AppendLine("=== WEATHER DATA ===");
            if (weatherData.Count > 0)
            {
                sb.AppendLine("Date,Temperature(°C),FeelsLike(°C),Humidity(%),Pressure(hPa),WindSpeed(m/s),Visibility(m),CloudCover(%),Condition");
                foreach (var w in weatherData)
                {
                    sb.AppendLine($"{w.RecordedAt:yyyy-MM-dd HH:mm},{w.Temperature},{w.FeelsLike},{w.Humidity},{w.Pressure},{w.WindSpeed},{w.Visibility},{w.CloudCover},{w.WeatherCondition}");
                }
            }
            else if (currentWeather != null)
            {
                sb.AppendLine("(Current weather — no historical data available)");
                sb.AppendLine("Date,Temperature(°C),FeelsLike(°C),Humidity(%),Pressure(hPa),WindSpeed(m/s),Visibility(m),CloudCover(%),Condition");
                sb.AppendLine($"{currentWeather.RecordedAt:yyyy-MM-dd HH:mm},{currentWeather.Temperature},{currentWeather.FeelsLike},{currentWeather.Humidity},{currentWeather.Pressure},{currentWeather.WindSpeed},{currentWeather.Visibility},{currentWeather.CloudCover},{currentWeather.WeatherCondition}");
            }
            else
            {
                sb.AppendLine("No weather data available.");
            }

            sb.AppendLine();

            // AQI data
            sb.AppendLine("=== AQI DATA ===");
            if (aqiData.Count > 0)
            {
                sb.AppendLine("Date,AQI,Category,PM2.5,PM10,O3,NO2,SO2,CO,DominantPollutant");
                foreach (var a in aqiData)
                {
                    sb.AppendLine($"{a.RecordedAt:yyyy-MM-dd HH:mm},{a.AQI},{a.Category},{a.PM25},{a.PM10},{a.O3},{a.NO2},{a.SO2},{a.CO},{a.DominantPollutant}");
                }
            }
            else
            {
                sb.AppendLine("No AQI data available.");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportToPdfAsync(int districtId, int days)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var district = await _db.Districts.FindAsync(districtId);
            var weatherData = await _db.WeatherSnapshots
                .Where(w => w.DistrictId == districtId && w.RecordedAt >= since)
                .OrderByDescending(w => w.RecordedAt)
                .ToListAsync();
            var aqiData = await _db.AQISnapshots
                .Where(a => a.DistrictId == districtId && a.RecordedAt >= since)
                .OrderByDescending(a => a.RecordedAt)
                .ToListAsync();

            // Fix 8: If no AQI data from DB, fetch from API
            if (aqiData.Count == 0 && district != null)
            {
                var start = DateTime.UtcNow.AddDays(-days);
                var end = DateTime.UtcNow;
                var apiData = await _aqiService.GetHistoricalFromApiAsync(district, start, end);
                aqiData = apiData.OrderByDescending(a => a.RecordedAt).ToList();
            }

            // Fix 8: If no weather data, fetch current
            WeatherSnapshot? currentWeather = null;
            if (weatherData.Count == 0 && district != null)
            {
                currentWeather = await _weatherService.GetCurrentWeatherAsync(district);
            }

            var cityName = district?.Name ?? "Unknown";
            var latestAqi = aqiData.FirstOrDefault();
            var latestWeather = weatherData.FirstOrDefault() ?? currentWeather;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"EnviroWatch — Air Quality & Weather Report")
                            .FontSize(18).Bold().FontColor(Colors.Green.Darken2);
                        col.Item().Text($"{cityName} | Last {days} Days | Generated: {DateTime.Now:dd MMM yyyy HH:mm}")
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        // Summary section
                        col.Item().PaddingVertical(10).Text("Current Summary").FontSize(14).Bold();

                        if (latestAqi != null)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });

                                table.Cell().Padding(5).Text("AQI").Bold();
                                table.Cell().Padding(5).Text($"{latestAqi.AQI} ({latestAqi.Category})");
                                table.Cell().Padding(5).Text("Dominant").Bold();
                                table.Cell().Padding(5).Text(latestAqi.DominantPollutant);
                            });
                        }

                        if (latestWeather != null)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });

                                table.Cell().Padding(5).Text("Temperature").Bold();
                                table.Cell().Padding(5).Text($"{latestWeather.Temperature}°C (Feels {latestWeather.FeelsLike}°C)");
                                table.Cell().Padding(5).Text("Humidity").Bold();
                                table.Cell().Padding(5).Text($"{latestWeather.Humidity}%");
                            });

                            if (weatherData.Count == 0)
                            {
                                col.Item().PaddingTop(5).Text("(Current weather — no historical data available)")
                                    .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                            }
                        }

                        // AQI History Table
                        col.Item().PaddingTop(15).Text("AQI History").FontSize(14).Bold();

                        if (aqiData.Count == 0)
                        {
                            col.Item().Text("No AQI data available.").FontSize(10).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Padding(3).Text("Date").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("AQI").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("Category").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("PM2.5").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("PM10").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("Dominant").Bold().FontSize(9);
                                });

                                foreach (var a in aqiData)
                                {
                                    table.Cell().Padding(3).Text(a.RecordedAt.ToString("dd/MM HH:mm")).FontSize(8);
                                    table.Cell().Padding(3).Text(a.AQI.ToString()).FontSize(8);
                                    table.Cell().Padding(3).Text(a.Category).FontSize(8);
                                    table.Cell().Padding(3).Text(a.PM25.ToString("F1")).FontSize(8);
                                    table.Cell().Padding(3).Text(a.PM10.ToString("F1")).FontSize(8);
                                    table.Cell().Padding(3).Text(a.DominantPollutant).FontSize(8);
                                }
                            });
                        }

                        // Weather History Table
                        col.Item().PaddingTop(15).Text("Weather History").FontSize(14).Bold();

                        if (weatherData.Count == 0 && currentWeather == null)
                        {
                            col.Item().Text("No weather data available.").FontSize(10).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            var weatherToDisplay = weatherData.Count > 0
                                ? weatherData
                                : new List<WeatherSnapshot> { currentWeather! };

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Padding(3).Text("Date").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("Temp°C").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("Humidity%").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("Wind m/s").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("Pressure").Bold().FontSize(9);
                                    header.Cell().Padding(3).Text("Condition").Bold().FontSize(9);
                                });

                                foreach (var w in weatherToDisplay)
                                {
                                    table.Cell().Padding(3).Text(w.RecordedAt.ToString("dd/MM HH:mm")).FontSize(8);
                                    table.Cell().Padding(3).Text(w.Temperature.ToString("F1")).FontSize(8);
                                    table.Cell().Padding(3).Text(w.Humidity.ToString()).FontSize(8);
                                    table.Cell().Padding(3).Text(w.WindSpeed.ToString("F1")).FontSize(8);
                                    table.Cell().Padding(3).Text(w.Pressure.ToString("F0")).FontSize(8);
                                    table.Cell().Padding(3).Text(w.WeatherCondition).FontSize(8);
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("EnviroWatch © 2026 | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}

namespace EnviroWatch.Services
{
    public class HealthRecommendation
    {
        public string Verdict { get; set; } = string.Empty;        // "Safe ✅", "Caution ⚠️", "Unsafe ❌"
        public string VerdictClass { get; set; } = string.Empty;    // CSS class: "safe", "caution", "unsafe"
        public string AQIAdvice { get; set; } = string.Empty;
        public string WeatherAdvice { get; set; } = string.Empty;
        public List<string> DosAndDonts { get; set; } = new();
        public double HeatIndex { get; set; }
        public string HeatIndexCategory { get; set; } = string.Empty;
        public string OverallSummary { get; set; } = string.Empty;
    }

    public interface IRecommendationService
    {
        HealthRecommendation GetRecommendation(int aqi, string aqiCategory, double temperature, int humidity, double windSpeed, string weatherCondition);
    }
}

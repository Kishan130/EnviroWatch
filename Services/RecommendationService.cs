namespace EnviroWatch.Services
{
    public class RecommendationService : IRecommendationService
    {
        public HealthRecommendation GetRecommendation(int aqi, string aqiCategory, double temperature, int humidity, double windSpeed, string weatherCondition)
        {
            var rec = new HealthRecommendation();

            // ===== Heat Index Calculation (Steadman's formula) =====
            double tempF = (temperature * 9.0 / 5.0) + 32.0;
            double hi = tempF;
            if (tempF >= 80)
            {
                hi = -42.379 + 2.04901523 * tempF + 10.14333127 * humidity
                    - 0.22475541 * tempF * humidity - 0.00683783 * tempF * tempF
                    - 0.05481717 * humidity * humidity + 0.00122874 * tempF * tempF * humidity
                    + 0.00085282 * tempF * humidity * humidity - 0.00000199 * tempF * tempF * humidity * humidity;
            }
            rec.HeatIndex = Math.Round((hi - 32) * 5.0 / 9.0, 1); // Convert back to Celsius

            rec.HeatIndexCategory = rec.HeatIndex switch
            {
                < 27 => "Normal",
                < 32 => "Caution",
                < 41 => "Extreme Caution",
                < 54 => "Danger",
                _ => "Extreme Danger"
            };

            // ===== AQI-based advice =====
            rec.AQIAdvice = aqi switch
            {
                <= 50 => "Air quality is excellent. Perfect for outdoor activities!",
                <= 100 => "Air quality is acceptable. Sensitive individuals should limit prolonged outdoor exertion.",
                <= 200 => "Air quality is moderate. Reduce prolonged outdoor exertion, especially for children and elderly.",
                <= 300 => "Air quality is poor. Everyone should reduce outdoor activities. Wear N95 masks outside.",
                <= 400 => "Air quality is very poor. Avoid all outdoor activities. Keep windows and doors closed.",
                _ => "Air quality is severe/hazardous. Stay indoors. Use air purifiers. Seek medical attention if experiencing breathing difficulty."
            };

            // ===== Weather-based advice =====
            var weatherAdvices = new List<string>();
            var condition = weatherCondition.ToLower();

            if (temperature > 40)
                weatherAdvices.Add("🔥 Extreme heat alert! Stay hydrated, avoid direct sun exposure between 11 AM - 4 PM.");
            else if (temperature > 35)
                weatherAdvices.Add("☀️ Very hot conditions. Drink plenty of water, wear light clothing and sunscreen.");
            else if (temperature < 10)
                weatherAdvices.Add("🥶 Cold weather! Wear warm clothing and layers.");
            else if (temperature < 5)
                weatherAdvices.Add("❄️ Freezing conditions! Use heavy winter clothing, beware of hypothermia.");

            if (humidity > 80)
                weatherAdvices.Add("💧 High humidity. You may feel hotter than actual temperature. Stay cool.");
            if (windSpeed > 10)
                weatherAdvices.Add("💨 Strong winds expected. Secure loose objects and avoid high-rise areas.");

            if (condition.Contains("rain") || condition.Contains("drizzle"))
                weatherAdvices.Add("🌧 Rain expected. Carry an umbrella and avoid waterlogged areas.");
            else if (condition.Contains("thunderstorm"))
                weatherAdvices.Add("⛈ Thunderstorm warning! Stay indoors, unplug electronics, avoid open areas.");
            else if (condition.Contains("snow"))
                weatherAdvices.Add("🌨 Snowfall expected. Drive carefully, wear warm waterproof clothing.");
            else if (condition.Contains("fog") || condition.Contains("mist") || condition.Contains("haze"))
                weatherAdvices.Add("🌫 Low visibility conditions. Drive slowly with fog lights, avoid highway travel.");

            rec.WeatherAdvice = weatherAdvices.Count > 0
                ? string.Join(" | ", weatherAdvices)
                : "☀️ Weather conditions are comfortable for outdoor activities.";

            // ===== Do's and Don'ts =====
            rec.DosAndDonts = new List<string>();

            if (aqi <= 50)
            {
                rec.DosAndDonts.AddRange(new[] {
                    "✅ Great day for outdoor exercise and walks",
                    "✅ Open windows for fresh air ventilation",
                    "✅ Perfect for morning/evening jogs",
                    "✅ Children can play outdoors freely"
                });
            }
            else if (aqi <= 100)
            {
                rec.DosAndDonts.AddRange(new[] {
                    "✅ Light outdoor activities are fine",
                    "⚠️ Sensitive groups should limit prolonged outdoor exercise",
                    "✅ Short walks and errands are safe",
                    "❌ Avoid intense outdoor workouts if you have respiratory conditions"
                });
            }
            else if (aqi <= 200)
            {
                rec.DosAndDonts.AddRange(new[] {
                    "⚠️ Limit prolonged outdoor exertion",
                    "⚠️ Wear a mask if going out for extended periods",
                    "❌ Avoid outdoor exercise and sports",
                    "✅ Short outdoor trips are acceptable",
                    "✅ Keep indoor air purifier running if available"
                });
            }
            else if (aqi <= 300)
            {
                rec.DosAndDonts.AddRange(new[] {
                    "❌ Avoid outdoor activities completely",
                    "❌ Do not exercise outdoors",
                    "✅ Wear N95/P100 mask if you must go out",
                    "✅ Keep all windows and doors closed",
                    "✅ Use air purifiers indoors",
                    "⚠️ Monitor for breathing difficulties"
                });
            }
            else
            {
                rec.DosAndDonts.AddRange(new[] {
                    "🚫 Stay indoors at all times",
                    "🚫 Do NOT open windows or doors",
                    "✅ Use air purifiers on maximum setting",
                    "✅ Wear N95 mask even indoors if no purifier",
                    "✅ Keep emergency contacts ready",
                    "⚠️ Seek medical help if experiencing coughing, wheezing, or breathlessness",
                    "⚠️ Elderly and children are at highest risk"
                });
            }

            // ===== Overall Verdict =====
            int riskScore = 0;

            // AQI risk contribution
            if (aqi <= 50) riskScore += 0;
            else if (aqi <= 100) riskScore += 1;
            else if (aqi <= 200) riskScore += 3;
            else if (aqi <= 300) riskScore += 5;
            else riskScore += 7;

            // Weather risk contribution
            if (temperature > 42 || temperature < 2) riskScore += 3;
            else if (temperature > 38 || temperature < 8) riskScore += 2;

            if (rec.HeatIndex > 41) riskScore += 2;

            if (condition.Contains("thunderstorm")) riskScore += 3;
            else if (condition.Contains("rain") || condition.Contains("snow")) riskScore += 1;

            if (windSpeed > 15) riskScore += 2;

            if (riskScore <= 1)
            {
                rec.Verdict = "Safe to Go Out ✅";
                rec.VerdictClass = "safe";
                rec.OverallSummary = "Conditions are favorable. Enjoy your outdoor activities!";
            }
            else if (riskScore <= 4)
            {
                rec.Verdict = "Caution Advised ⚠️";
                rec.VerdictClass = "caution";
                rec.OverallSummary = "Outdoor activities possible with precautions. Sensitive individuals should take extra care.";
            }
            else
            {
                rec.Verdict = "Stay Indoors ❌";
                rec.VerdictClass = "unsafe";
                rec.OverallSummary = "Conditions are unfavorable for outdoor activities. Please stay indoors and take necessary precautions.";
            }

            return rec;
        }
    }
}

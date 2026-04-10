# 🌍 EnviroWatch: Real-time AQI & Weather Monitoring Dashboard

![Status](https://img.shields.io/badge/Status-Active-brightgreen)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-blue)
![License](https://img.shields.io/badge/License-MIT-orange)

**EnviroWatch** is a modern, premium web application designed to track and visualize environmental data across India. Built with .NET 8, it provides users with real-time Air Quality Index (AQI) and weather data, historical insights, and advanced reporting capabilities.

---

## ✨ Features

- **📊 Comprehensive Dashboard**: Monitor AQI level, Temperature, Humidity, and Pressure in a single unified view.
- **📍 Location Intelligence**: Search for environmental data across various districts in India.
- **🔐 Secure Authentication**: Integrated with Google and GitHub OAuth for a seamless login experience.
- **📑 Professional Reporting**: Generate detailed PDF reports of environmental metrics using QuestPDF.
- **📱 Responsive Design**: A stunning SaaS-style UI that works perfectly on desktop and mobile devices.
- **🤖 Background Sync**: Automated background services to keep environmental snapshots up to date.

---

## 🚀 Tech Stack

- **Backend**: [ASP.NET Core 8.0](https://dotnet.microsoft.com/en-us/apps/aspnet)
- **Database**: SQL Server with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- **Frontend**: Razor Views, Vanilla CSS (Premium/Modern UI)
- **Authentication**: ASP.NET Core Identity & OAuth (Google/GitHub)
- **External APIs**: 
  - [OpenWeatherMap](https://openweathermap.org/api)
  - [IQAir](https://www.iqair.com/air-pollution-data-api)
- **Reporting**: [QuestPDF](https://www.questpdf.com/)

---

## 🛠️ Get Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or Express)
- API Keys for OpenWeatherMap and IQAir

### Setup
1. **Clone the Repository**:
   ```bash
   git clone https://github.com/Kishan130/EnviroWatch.git
   cd EnviroWatch
   ```

2. **Configure Secrets**:
   - Create a copy of `appsettings.template.json` and name it `appsettings.json`.
   - Fill in your API keys and database connection string.
   - Alternatively, use .NET User Secrets:
     ```bash
     dotnet user-secrets set "OpenWeatherMap:ApiKey" "your_key"
     dotnet user-secrets set "IQAir:ApiKey" "your_key"
     ```

3. **Database Migration**:
   ```bash
   dotnet ef database update
   ```

4. **Run the App**:
   ```bash
   dotnet run
   ```

---

## 👥 Authors

- **Kishan Vachhani** - Lead Developer
- **Rajnish Sinh** - Developer Collaborator

---

## 📄 Documentation

Project reports and presentations can be found in the `/Documents` folder.

---

*Developed with ❤️ as a D2D Semester 6 Mini Project.*

let tempChart = null;
let aqiChart = null;

/* ---------- PAGE LOAD ---------- */
$(function () {

    // District selection change (modern delegated binding)
    $(document).on("change", "#districtSelect", function () {
        const districtId = $(this).val();
        if (districtId) loadDistrictData(districtId);
    });

    // Refresh button
    $(document).on("click", "#btnRefresh", function () {
        const districtId = $("#districtSelect").val();
        if (districtId) {
            loadDistrictData(districtId);
        } else {
            alert("Please select a district first");
        }
    });
});


/* ---------- MAIN DATA LOADER ---------- */
function loadDistrictData(districtId) {

    $("#loading").show();
    $("#dataSection").hide();

    // Run all API calls together
    const weatherReq = $.get(`/api/weather/current/${districtId}`);
    const aqiReq = $.get(`/api/aqi/current/${districtId}`);
    const weatherHistReq = $.get(`/api/weather/historical/${districtId}/7`);
    const aqiHistReq = $.get(`/api/aqi/historical/${districtId}/7`);

    $.when(weatherReq, aqiReq, weatherHistReq, aqiHistReq)
        .done(function (weatherRes, aqiRes, weatherHistRes, aqiHistRes) {

            /* Weather current */
            const weather = weatherRes[0];
            $("#temp").text(weather.temperature?.toFixed(1) ?? "N/A");
            $("#humidity").text(weather.humidity?.toFixed(0) ?? "N/A");
            $("#windSpeed").text(weather.windSpeed?.toFixed(1) ?? "N/A");
            $("#condition").text(weather.weatherCondition || "N/A");

            /* AQI current */
            const response = aqiRes[0];
            const aqi = response?.data?.aqi ?? "N/A";

            $("#aqiValue")
                .text(aqi)
                .css("background-color", response?.color || "#999");

            $("#aqiCategory").text(response?.category || "Unknown");
            $("#pm25").text(response?.data?.pm25?.toFixed(1) ?? "N/A");
            $("#pm10").text(response?.data?.pm10?.toFixed(1) ?? "N/A");

            /* Charts */
            renderTempChart(weatherHistRes[0]);
            renderAQIChart(aqiHistRes[0]);

            $("#loading").hide();
            $("#dataSection").fadeIn();
        })
        .fail(function () {
            $("#loading").hide();
            alert("Failed to load district data. Server not responding.");
        });
}


/* ---------- TEMPERATURE CHART ---------- */
function renderTempChart(data) {

    if (!data || data.length === 0) return;

    const labels = data.map(d =>
        new Date(d.timestamp).toLocaleDateString()
    );

    const temps = data.map(d => d.temperature);

    if (tempChart) {
        tempChart.destroy();
        tempChart = null;
    }

    const ctx = document.getElementById("tempChart")?.getContext("2d");
    if (!ctx) return;

    tempChart = new Chart(ctx, {
        type: "line",
        data: {
            labels: labels,
            datasets: [{
                label: "Temperature (°C)",
                data: temps,
                borderColor: "#FF6384",
                backgroundColor: "rgba(255, 99, 132, 0.2)",
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: true } }
        }
    });
}


/* ---------- AQI CHART ---------- */
function renderAQIChart(data) {

    if (!data || data.length === 0) return;

    const labels = data.map(d =>
        new Date(d.timestamp).toLocaleDateString()
    );

    const aqis = data.map(d => d.aqi);

    if (aqiChart) {
        aqiChart.destroy();
        aqiChart = null;
    }

    const ctx = document.getElementById("aqiChart")?.getContext("2d");
    if (!ctx) return;

    aqiChart = new Chart(ctx, {
        type: "bar",
        data: {
            labels: labels,
            datasets: [{
                label: "AQI",
                data: aqis,
                backgroundColor: "#36A2EB"
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: true } }
        }
    });
}

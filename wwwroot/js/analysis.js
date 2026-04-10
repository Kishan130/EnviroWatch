// =====================================================
// EnviroWatch — Analysis Module
// Chart.js rendering, city analysis, comparison
// =====================================================

let charts = {};
let allDistrictsAnalysis = [];

$(document).ready(function () {
    loadDistrictsList();
    loadIndiaOverview();

    $('#citySelector').on('change', function () {
        const id = $(this).val();
        if (id) {
            loadCityAnalysis(id);
        } else {
            showIndiaOverview();
        }
    });

    $('#periodSelector').on('change', function () {
        const cityId = $('#citySelector').val();
        if (cityId) loadCityAnalysis(cityId);
    });

    // Dynamic comparison: add/remove city + compare button
    $('#addCompareCity').on('click', addCompareCitySelector);
    $('#compareBtn').on('click', compareDistricts);
});

// ===== Load Districts for Selector =====
async function loadDistrictsList() {
    try {
        const data = await $.getJSON('/api/districts');
        allDistrictsAnalysis = data;

        // Sort all cities A-Z (Fix 5: no optgroups)
        const sorted = [...data].sort((a, b) => a.name.localeCompare(b.name));

        const $sel = $('#citySelector');
        sorted.forEach(d => {
            $sel.append(`<option value="${d.id}">${d.name} (${d.state})</option>`);
        });

        // Populate initial comparison selectors
        populateCompareSelector($('#compareCityList .compare-city-select'));
    } catch (err) {
        console.error('Failed to load districts:', err);
    }
}

function populateCompareSelector($selectors) {
    if (!$selectors || $selectors.length === 0) return;
    const sorted = [...allDistrictsAnalysis].sort((a, b) => a.name.localeCompare(b.name));
    $selectors.each(function () {
        const $sel = $(this);
        if ($sel.find('option').length > 1) return; // already populated
        sorted.forEach(d => {
            $sel.append(`<option value="${d.id}">${d.name}</option>`);
        });
    });
}

// ===== Dynamic Comparison City Add/Remove =====
function addCompareCitySelector() {
    const count = $('#compareCityList .compare-city-row').length;
    if (count >= 10) {
        return; // max 10 cities
    }

    const sorted = [...allDistrictsAnalysis].sort((a, b) => a.name.localeCompare(b.name));
    const options = sorted.map(d => `<option value="${d.id}">${d.name}</option>`).join('');

    const row = $(`
        <div class="compare-city-row" style="display:flex; gap:8px; align-items:center; margin-bottom:8px;">
            <select class="select-styled compare-city-select" style="min-width: 150px; flex:1;">
                <option value="">Select City</option>
                ${options}
            </select>
            <button class="btn btn-danger btn-sm remove-compare-city" type="button" title="Remove">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `);

    row.find('.remove-compare-city').on('click', function () {
        $(this).closest('.compare-city-row').remove();
        updateAddBtnState();
    });

    $('#compareCityList').append(row);
    updateAddBtnState();
}

function updateAddBtnState() {
    const count = $('#compareCityList .compare-city-row').length;
    $('#addCompareCity').prop('disabled', count >= 10);
    if (count >= 10) {
        $('#addCompareCity').text('Max 10');
    } else {
        $('#addCompareCity').html('<i class="fas fa-plus"></i> Add City');
    }
}

// ===== India Overview =====
async function loadIndiaOverview() {
    try {
        const data = await $.getJSON('/api/analysis/india-overview');

        // Stats
        $('#statAvgAqi').text(Math.round(data.nationalAvgAQI || 0))
            .css('color', getAQIColorAnalysis(data.nationalAvgAQI));
        $('#statAvgTemp').text(`${(data.nationalAvgTemp || 0).toFixed(1)}°C`);
        $('#statAvgHumidity').text(`${Math.round(data.nationalAvgHumidity || 0)}%`);
        $('#statCities').text(data.totalCities || 35);

        // Metro AQI Bar Chart
        if (data.metroData && data.metroData.length > 0) {
            renderMetroAqiChart(data.metroData);
        }

        // Top Polluted Table
        if (data.topPolluted && data.topPolluted.length > 0) {
            renderTopPollutedTable(data.topPolluted);
        }

        // National Trends
        if (data.dailyAvgAqi && data.dailyAvgAqi.length > 0) {
            renderNationalAqiTrend(data.dailyAvgAqi);
        } else {
            // Show "No trend data" message in chart area
            const ctx = document.getElementById('nationalAqiTrendChart');
            if (ctx) {
                destroyChart('nationalAqi');
                const parent = ctx.parentElement;
                if (!parent.querySelector('.no-data-message')) {
                    parent.insertAdjacentHTML('beforeend', '<div class="no-data-message" style="position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);color:var(--text-muted);font-size:0.85rem;text-align:center;"><i class="fas fa-chart-area" style="font-size:1.5rem;margin-bottom:8px;display:block;opacity:0.5;"></i>AQI trend data loading from API...</div>');
                }
            }
        }
        if (data.dailyAvgTemp && data.dailyAvgTemp.length > 0) {
            renderNationalTempTrend(data.dailyAvgTemp);
        }
    } catch (err) {
        console.error('Failed to load India overview:', err);
    }
}

function showIndiaOverview() {
    $('#indiaOverview').show();
    $('#cityDetail').hide();
    $('#exportBtns').hide();
    loadIndiaOverview();
}

// ===== City Analysis =====
async function loadCityAnalysis(districtId) {
    const days = $('#periodSelector').val();

    $('#indiaOverview').hide();
    $('#cityDetail').show();
    $('#exportBtns').show();

    // Update export links
    $('#exportCsvBtn').attr('href', `/export/csv/${districtId}?days=${days}`);
    $('#exportPdfBtn').attr('href', `/export/pdf/${districtId}?days=${days}`);

    try {
        const data = await $.getJSON(`/api/analysis/city/${districtId}?days=${days}`);

        $('#detailCityName').text(data.district.name);

        // Stats
        $('#cityAvgAqi').text(Math.round(data.stats.avgAQI)).css('color', getAQIColorAnalysis(data.stats.avgAQI));
        $('#cityMaxAqi').text(data.stats.maxAQI);
        $('#cityMinAqi').text(data.stats.minAQI);
        $('#cityAvgTemp').text(`${data.stats.avgTemp.toFixed(1)}°C`);
        $('#cityMaxTemp').text(`${data.stats.maxTemp.toFixed(1)}°C`);
        $('#cityDataPoints').text(data.stats.dataPoints);

        // Charts
        renderCityAqiTrend(data.aqiTrend);
        renderPollutantBreakdown(data.pollutantTrend);
        renderCategoryDistribution(data.categoryDistribution);
        renderPollutantAvgChart(data.pollutantAvgs);

        // Fix 6: Weather charts — fallback to current weather
        if (data.weatherTrend && data.weatherTrend.length > 0) {
            // Hide fallback, show charts
            $('#noWeatherData').hide();
            $('#cityTempHumidityChart').closest('.chart-card').show();
            $('#cityWindChart').closest('.chart-card').show();
            renderCityTempHumidity(data.weatherTrend);
            renderCityWindChart(data.weatherTrend);
        } else {
            // No weather history — show current weather fallback
            destroyChart('cityTempHumidity');
            destroyChart('cityWind');
            $('#cityTempHumidityChart').closest('.chart-card').hide();
            $('#cityWindChart').closest('.chart-card').hide();

            if (data.currentWeather) {
                const cw = data.currentWeather;
                $('#noWeatherData').show().html(`
                    <div class="chart-card-title"><i class="fas fa-cloud-sun"></i> Current Weather Conditions</div>
                    <div style="color: var(--text-muted); font-size: 0.8rem; margin-bottom: 12px;">
                        <i class="fas fa-info-circle"></i> Historical weather data not available yet — showing current conditions.
                    </div>
                    <div class="weather-details-grid">
                        <div class="weather-detail">
                            <div class="weather-detail-label"><i class="fas fa-temperature-high"></i> Temperature</div>
                            <div class="weather-detail-value">${cw.temperature?.toFixed(1) ?? '--'}°C</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label"><i class="fas fa-tint"></i> Humidity</div>
                            <div class="weather-detail-value">${cw.humidity ?? '--'}%</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label"><i class="fas fa-wind"></i> Wind</div>
                            <div class="weather-detail-value">${cw.windSpeed?.toFixed(1) ?? '--'} m/s</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label"><i class="fas fa-cloud"></i> Condition</div>
                            <div class="weather-detail-value">${cw.weatherCondition ?? '--'}</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label"><i class="fas fa-compress-arrows-alt"></i> Pressure</div>
                            <div class="weather-detail-value">${cw.pressure?.toFixed(0) ?? '--'} hPa</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label"><i class="fas fa-eye"></i> Visibility</div>
                            <div class="weather-detail-value">${cw.visibility ? (cw.visibility / 1000).toFixed(1) : '--'} km</div>
                        </div>
                    </div>
                `);
            } else {
                $('#noWeatherData').show().html(`
                    <div style="text-align:center; padding: 32px; color: var(--text-muted);">
                        <i class="fas fa-cloud" style="font-size:2rem; margin-bottom:8px; opacity:0.4; display:block;"></i>
                        No weather data available for this city.
                    </div>
                `);
            }
        }
    } catch (err) {
        console.error('Failed to load city analysis:', err);
    }
}

// ===== Chart Rendering Functions =====

function getChartColors() {
    const style = getComputedStyle(document.documentElement);
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    return {
        text: style.getPropertyValue('--text-muted').trim() || '#94a3b8',
        grid: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)',
        legend: style.getPropertyValue('--text-secondary').trim() || '#94a3b8'
    };
}

function getChartDefaults() {
    const c = getChartColors();
    return {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                labels: { color: c.legend, font: { family: 'Inter', size: 11 } }
            }
        },
        scales: {
            x: {
                ticks: { color: c.text, font: { family: 'Inter', size: 10 } },
                grid: { color: c.grid }
            },
            y: {
                ticks: { color: c.text, font: { family: 'Inter', size: 10 } },
                grid: { color: c.grid }
            }
        }
    };
}

const chartDefaults = getChartDefaults();

function destroyChart(chartKey) {
    if (charts[chartKey]) {
        charts[chartKey].destroy();
        charts[chartKey] = null;
    }
}

function renderMetroAqiChart(metroData) {
    destroyChart('metroAqi');
    const sorted = [...metroData].sort((a, b) => b.aqi - a.aqi);
    const ctx = document.getElementById('metroAqiChart').getContext('2d');

    charts.metroAqi = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: sorted.map(d => d.name),
            datasets: [{
                label: 'AQI',
                data: sorted.map(d => d.aqi),
                backgroundColor: sorted.map(d => getAQIColorAnalysis(d.aqi) + '99'),
                borderColor: sorted.map(d => getAQIColorAnalysis(d.aqi)),
                borderWidth: 1,
                borderRadius: 4
            }]
        },
        options: {
            ...chartDefaults,
            indexAxis: 'y',
            plugins: {
                ...chartDefaults.plugins,
                legend: { display: false }
            }
        }
    });
}

function renderTopPollutedTable(data) {
    const tbody = $('#topPollutedBody');
    tbody.empty();
    data.forEach((d, i) => {
        tbody.append(`
            <tr>
                <td class="rank-number">${i + 1}</td>
                <td>${d.cityName || d.name}</td>
                <td style="color: ${getAQIColorAnalysis(d.aqi)}">${d.aqi}</td>
                <td>${d.category}</td>
                <td>${d.dominantPollutant || 'N/A'}</td>
            </tr>
        `);
    });
}

function renderNationalAqiTrend(data) {
    destroyChart('nationalAqi');
    // Remove any "no data" message
    const chartEl = document.getElementById('nationalAqiTrendChart');
    const noDataMsg = chartEl?.parentElement?.querySelector('.no-data-message');
    if (noDataMsg) noDataMsg.remove();

    const ctx = chartEl.getContext('2d');

    charts.nationalAqi = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.date),
            datasets: [{
                label: 'Avg AQI',
                data: data.map(d => d.avgAQI),
                borderColor: '#22d3ee',
                backgroundColor: 'rgba(34, 211, 238, 0.15)',
                fill: true,
                tension: 0.4,
                pointRadius: 4,
                pointBackgroundColor: '#22d3ee'
            }]
        },
        options: chartDefaults
    });
}

function renderNationalTempTrend(data) {
    destroyChart('nationalTemp');
    const ctx = document.getElementById('nationalTempTrendChart').getContext('2d');

    charts.nationalTemp = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.date),
            datasets: [{
                label: 'Avg Temperature (°C)',
                data: data.map(d => d.avgTemp),
                borderColor: '#f59e0b',
                backgroundColor: 'rgba(245, 158, 11, 0.15)',
                fill: true,
                tension: 0.4,
                pointRadius: 4,
                pointBackgroundColor: '#f59e0b'
            }]
        },
        options: chartDefaults
    });
}

function renderCityAqiTrend(data) {
    destroyChart('cityAqi');
    if (!data || data.length === 0) return;
    const ctx = document.getElementById('cityAqiTrendChart').getContext('2d');

    charts.cityAqi = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.date),
            datasets: [{
                label: 'AQI',
                data: data.map(d => d.aqi),
                borderColor: '#22d3ee',
                backgroundColor: 'rgba(34, 211, 238, 0.1)',
                fill: true,
                tension: 0.3,
                pointRadius: 3,
                pointBackgroundColor: data.map(d => getAQIColorAnalysis(d.aqi))
            }]
        },
        options: chartDefaults
    });
}

function renderCityTempHumidity(data) {
    destroyChart('cityTempHumidity');
    if (!data || data.length === 0) return;
    const ctx = document.getElementById('cityTempHumidityChart').getContext('2d');

    charts.cityTempHumidity = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.date),
            datasets: [
                {
                    label: 'Temperature (°C)',
                    data: data.map(d => d.temperature),
                    borderColor: '#f59e0b',
                    backgroundColor: 'rgba(245, 158, 11, 0.1)',
                    fill: false,
                    tension: 0.3,
                    yAxisID: 'y'
                },
                {
                    label: 'Humidity (%)',
                    data: data.map(d => d.humidity),
                    borderColor: '#60a5fa',
                    backgroundColor: 'rgba(96, 165, 250, 0.1)',
                    fill: false,
                    tension: 0.3,
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            ...chartDefaults,
            scales: {
                ...chartDefaults.scales,
                y: { ...chartDefaults.scales.y, position: 'left', title: { display: true, text: '°C', color: '#64748b' } },
                y1: { ...chartDefaults.scales.y, position: 'right', title: { display: true, text: '%', color: '#64748b' }, grid: { drawOnChartArea: false } }
            }
        }
    });
}

function renderPollutantBreakdown(data) {
    destroyChart('pollutantBreakdown');
    if (!data || data.length === 0) return;
    const ctx = document.getElementById('pollutantBreakdownChart').getContext('2d');

    charts.pollutantBreakdown = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.date),
            datasets: [
                { label: 'PM2.5', data: data.map(d => d.pm25), borderColor: '#ef4444', tension: 0.3 },
                { label: 'PM10', data: data.map(d => d.pm10), borderColor: '#f59e0b', tension: 0.3 },
                { label: 'O₃', data: data.map(d => d.o3), borderColor: '#22d3ee', tension: 0.3 },
                { label: 'NO₂', data: data.map(d => d.no2), borderColor: '#a855f7', tension: 0.3 },
                { label: 'SO₂', data: data.map(d => d.so2), borderColor: '#10b981', tension: 0.3 },
                { label: 'CO', data: data.map(d => d.co), borderColor: '#64748b', tension: 0.3 }
            ]
        },
        options: {
            ...chartDefaults,
            plugins: {
                ...chartDefaults.plugins,
                legend: { labels: { color: '#94a3b8', font: { family: 'Inter', size: 10 }, boxWidth: 12 } }
            }
        }
    });
}

function renderCategoryDistribution(data) {
    destroyChart('categoryDist');
    if (!data || data.length === 0) return;
    const ctx = document.getElementById('categoryDistChart').getContext('2d');

    const colorMap = {
        'Good': '#00e400', 'Satisfactory': '#92d050', 'Moderate': '#ffff00',
        'Poor': '#ff7e00', 'Very Poor': '#ff0000', 'Severe': '#8f3f97'
    };

    charts.categoryDist = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.map(d => d.category),
            datasets: [{
                data: data.map(d => d.count),
                backgroundColor: data.map(d => colorMap[d.category] || '#64748b'),
                borderColor: document.documentElement.getAttribute('data-theme') === 'dark' ? 'rgba(15, 23, 42, 0.8)' : 'rgba(255, 255, 255, 0.8)',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { color: '#94a3b8', font: { family: 'Inter', size: 11 }, padding: 15 }
                }
            }
        }
    });
}

function renderCityWindChart(data) {
    destroyChart('cityWind');
    if (!data || data.length === 0) return;
    const ctx = document.getElementById('cityWindChart').getContext('2d');

    charts.cityWind = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.date),
            datasets: [{
                label: 'Wind Speed (m/s)',
                data: data.map(d => d.windSpeed),
                borderColor: '#10b981',
                backgroundColor: 'rgba(16, 185, 129, 0.1)',
                fill: true,
                tension: 0.3,
                pointRadius: 3
            }]
        },
        options: chartDefaults
    });
}

function renderPollutantAvgChart(data) {
    destroyChart('pollutantAvg');
    if (!data) return;
    const ctx = document.getElementById('pollutantAvgChart').getContext('2d');

    charts.pollutantAvg = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['PM2.5', 'PM10', 'O₃', 'NO₂', 'SO₂', 'CO'],
            datasets: [{
                label: 'Average µg/m³',
                data: [data.pm25, data.pm10, data.o3, data.no2, data.so2, data.co],
                backgroundColor: ['#ef444499', '#f59e0b99', '#22d3ee99', '#a855f799', '#10b98199', '#64748b99'],
                borderColor: ['#ef4444', '#f59e0b', '#22d3ee', '#a855f7', '#10b981', '#64748b'],
                borderWidth: 1,
                borderRadius: 4
            }]
        },
        options: {
            ...chartDefaults,
            plugins: { ...chartDefaults.plugins, legend: { display: false } }
        }
    });
}

// ===== City Comparison (Fix 7: dynamic add/remove) =====
async function compareDistricts() {
    const ids = [];
    $('#compareCityList .compare-city-select').each(function () {
        const val = $(this).val();
        if (val && !ids.includes(val)) ids.push(val);
    });

    if (ids.length === 0) return;

    // Include current city if viewing one
    const currentCity = $('#citySelector').val();
    if (currentCity && !ids.includes(currentCity)) ids.unshift(currentCity);

    try {
        const data = await $.getJSON(`/api/analysis/comparison?ids=${ids.join(',')}`);
        const $container = $('#comparisonCards');
        $container.empty();

        data.forEach(d => {
            $container.append(`
                <div class="comparison-city-card glass-card">
                    <h4 style="margin-bottom: 12px;">${d.name}</h4>
                    <div class="aqi-big-value" style="font-size: 2rem; color: ${getAQIColorAnalysis(d.aqi)}">${d.aqi}</div>
                    <div class="aqi-category-badge aqi-bg-${getAQICategoryClassAnalysis(d.category)}" style="margin: 8px 0;">${d.category}</div>
                    <div class="weather-details-grid" style="margin-top: 12px;">
                        <div class="weather-detail">
                            <div class="weather-detail-label">PM2.5</div>
                            <div class="weather-detail-value">${(d.pm25 || 0).toFixed(1)}</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label">Temp</div>
                            <div class="weather-detail-value">${(d.temperature || 0).toFixed(1)}°C</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label">Humidity</div>
                            <div class="weather-detail-value">${d.humidity || 0}%</div>
                        </div>
                        <div class="weather-detail">
                            <div class="weather-detail-label">Wind</div>
                            <div class="weather-detail-value">${(d.windSpeed || 0).toFixed(1)} m/s</div>
                        </div>
                    </div>
                </div>
            `);
        });
    } catch (err) {
        console.error('Comparison failed:', err);
    }
}

// ===== Auto-Refresh =====
function refreshData() {
    const cityId = $('#citySelector').val();
    if (cityId) {
        loadCityAnalysis(cityId);
    } else {
        loadIndiaOverview();
    }
}

// ===== Utility =====
function getAQIColorAnalysis(aqi) {
    if (aqi <= 50) return '#00e400';
    if (aqi <= 100) return '#92d050';
    if (aqi <= 200) return '#ffff00';
    if (aqi <= 300) return '#ff7e00';
    if (aqi <= 400) return '#ff0000';
    return '#8f3f97';
}

function getAQICategoryClassAnalysis(category) {
    const map = {
        'Good': 'good', 'Satisfactory': 'satisfactory', 'Moderate': 'moderate',
        'Poor': 'poor', 'Very Poor': 'very-poor', 'Severe': 'severe'
    };
    return map[category] || 'moderate';
}

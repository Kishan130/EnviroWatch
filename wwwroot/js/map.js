// =====================================================
// EnviroWatch — Interactive Map Module
// Leaflet.js + MarkerCluster + AQI-colored markers + Heatmap
// =====================================================

let map, markers = [], heatLayer = null, allDistricts = [];
let selectedMarker = null, selectedDistrictId = null;
let heatmapVisible = false;
let markerCluster = null;

// ===== Initialize Map =====
$(document).ready(function () {
    initMap();
    loadDistricts();
    setupSearch();
    setupControls();
});

function initMap() {
    map = L.map('mapView', {
        center: [22.5, 82.0],   // Center of India
        zoom: 5,
        minZoom: 4,
        maxZoom: 12,
        zoomControl: true
    });

    // Light map tiles (CartoDB Positron)
    var lightTiles = L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '© OpenStreetMap contributors © CARTO',
        subdomains: 'abcd',
        maxZoom: 19
    });

    // Dark map tiles (CartoDB Dark Matter)
    var darkTiles = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '© OpenStreetMap contributors © CARTO',
        subdomains: 'abcd',
        maxZoom: 19
    });

    // Apply current theme's tiles
    var currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    if (currentTheme === 'dark') {
        darkTiles.addTo(map);
    } else {
        lightTiles.addTo(map);
    }

    // Theme change handler — called from _Layout.cshtml
    window.onThemeChanged = function(theme) {
        if (theme === 'dark') {
            if (map.hasLayer(lightTiles)) map.removeLayer(lightTiles);
            if (!map.hasLayer(darkTiles)) darkTiles.addTo(map);
        } else {
            if (map.hasLayer(darkTiles)) map.removeLayer(darkTiles);
            if (!map.hasLayer(lightTiles)) lightTiles.addTo(map);
        }
    };

    // Initialize MarkerCluster group
    markerCluster = L.markerClusterGroup({
        maxClusterRadius: 50,
        spiderfyOnMaxZoom: true,
        showCoverageOnHover: false,
        zoomToBoundsOnClick: true,
        disableClusteringAtZoom: 10,
        iconCreateFunction: function (cluster) {
            var childMarkers = cluster.getAllChildMarkers();
            var aqiValues = childMarkers
                .map(m => m.districtData?._aqiValue)
                .filter(v => v != null && v > 0);
            var avgAqi = aqiValues.length > 0
                ? Math.round(aqiValues.reduce((a, b) => a + b, 0) / aqiValues.length)
                : 0;
            var color = avgAqi > 0 ? getAQIColor(avgAqi) : 'rgba(100,100,100,0.6)';
            var size = childMarkers.length > 100 ? 56 : childMarkers.length > 50 ? 48 : 40;

            return L.divIcon({
                html: `<div class="aqi-cluster-icon" style="
                    width:${size}px; height:${size}px;
                    background:${color};
                    border-radius:50%;
                    display:flex; align-items:center; justify-content:center;
                    color:#fff; font-weight:700; font-size:0.75rem;
                    border: 2px solid rgba(255,255,255,0.3);
                    box-shadow: 0 0 12px ${color};">
                    ${childMarkers.length}
                </div>`,
                className: 'aqi-cluster-container',
                iconSize: L.point(size, size)
            });
        }
    });
    map.addLayer(markerCluster);

    // Lazy load AQI when map moves
    map.on('moveend', lazyColorVisibleMarkers);
}

// ===== Lazy AQI Loading: Only fetch for visible markers =====
let coloredMarkerIds = new Set();
let lazyColorTimer = null;

function lazyColorVisibleMarkers() {
    clearTimeout(lazyColorTimer);
    lazyColorTimer = setTimeout(() => {
        const bounds = map.getBounds();
        markers.forEach(m => {
            const d = m.districtData;
            if (!d) return;
            if (coloredMarkerIds.has(d.id)) return;
            if (bounds.contains(m.getLatLng())) {
                const size = d.isMetroCity ? 32 : 24;
                fetchAndColorMarker(d, m, size);
                coloredMarkerIds.add(d.id);
            }
        });
    }, 300);
}

// ===== Load Districts & Create Markers =====
async function loadDistricts() {
    try {
        const response = await $.getJSON('/api/districts');
        allDistricts = response;
        createMarkers(allDistricts);
    } catch (err) {
        console.error('Failed to load districts:', err);
    }
}

function createMarkers(districts) {
    // Clear existing markers
    markerCluster.clearLayers();
    markers = [];
    coloredMarkerIds.clear();

    districts.forEach(d => {
        const marker = createAQIMarker(d);
        markers.push(marker);
    });

    // Color metro markers immediately
    markers.forEach(m => {
        const d = m.districtData;
        if (d && d.isMetroCity) {
            const size = 32;
            fetchAndColorMarker(d, m, size);
            coloredMarkerIds.add(d.id);
        }
    });

    // Lazy color visible non-metro markers
    lazyColorVisibleMarkers();
}

function createAQIMarker(district) {
    const size = district.isMetroCity ? 32 : 24;

    const icon = L.divIcon({
        className: 'aqi-marker-container',
        html: `<div class="aqi-marker" 
                    style="width:${size}px; height:${size}px; background: rgba(100,100,100,0.6);"
                    data-district-id="${district.id}">
                    <span style="font-size: ${size > 28 ? '0.7rem' : '0.6rem'}">•</span>
               </div>`,
        iconSize: [size, size],
        iconAnchor: [size / 2, size / 2]
    });

    const marker = L.marker([district.latitude, district.longitude], { icon });

    // Add to cluster group instead of directly to map
    markerCluster.addLayer(marker);

    marker.districtData = district;

    marker.on('click', () => {
        selectCity(district.id, marker);
    });

    // Tooltip
    marker.bindTooltip(district.name, {
        permanent: false,
        direction: 'top',
        className: 'map-tooltip',
        offset: [0, -size / 2]
    });

    return marker;
}

async function fetchAndColorMarker(district, marker, size) {
    try {
        const data = await $.getJSON(`/api/district/${district.id}/live`);
        if (data.aqi) {
            const color = getAQIColor(data.aqi.aqi);
            const markerEl = marker.getElement()?.querySelector('.aqi-marker');
            if (markerEl) {
                markerEl.style.background = color;
                markerEl.innerHTML = `<span style="font-size: ${size > 28 ? '0.7rem' : '0.6rem'}">${data.aqi.aqi}</span>`;
            }

            // Store for heatmap and cluster icon
            district._aqiValue = data.aqi.aqi;
        }
    } catch (err) {
        // Silently fail for initial coloring
    }
}

// ===== Select City =====
async function selectCity(districtId, marker) {
    selectedDistrictId = districtId;

    // Remove previous selection
    if (selectedMarker) {
        const prevEl = selectedMarker.getElement()?.querySelector('.aqi-marker');
        if (prevEl) prevEl.classList.remove('selected');
    }

    // Mark new selection
    if (marker) {
        selectedMarker = marker;
        const el = marker.getElement()?.querySelector('.aqi-marker');
        if (el) el.classList.add('selected');
        map.flyTo(marker.getLatLng(), 8, { duration: 0.8 });
    }

    // Show loading
    $('#panelPlaceholder').hide();
    $('#cityInfo').show().addClass('fade-in');

    try {
        const data = await $.getJSON(`/api/district/${districtId}/live`);
        updateInfoPanel(data);
    } catch (err) {
        console.error('Failed to fetch live data:', err);
    }
}

// ===== Update Info Panel =====
function updateInfoPanel(data) {
    // City name
    $('#cityName').text(data.district.name);
    $('#cityState').text(data.district.state);

    // AQI data
    if (data.aqi) {
        const aqi = data.aqi;
        const aqiColor = getAQIColor(aqi.aqi);
        $('#aqiValue').text(aqi.aqi).css('color', aqiColor);
        $('#aqiRing').css('color', aqiColor);
        const catClass = getAQICategoryClass(aqi.category);
        $('#aqiCategory').text(aqi.category)
            .attr('class', `aqi-category-badge aqi-bg-${catClass}`);

        $('#pm25Val').text(aqi.pm25?.toFixed(1) ?? '--');
        $('#pm10Val').text(aqi.pm10?.toFixed(1) ?? '--');
        $('#o3Val').text(aqi.o3?.toFixed(1) ?? '--');
        $('#no2Val').text(aqi.no2?.toFixed(1) ?? '--');
        $('#so2Val').text(aqi.so2?.toFixed(1) ?? '--');
        $('#coVal').text(aqi.co?.toFixed(1) ?? '--');
        $('#dominantPollutant').text(aqi.dominantPollutant || '--');
    }

    // Weather data
    if (data.weather) {
        const w = data.weather;
        $('#tempValue').text(w.temperature?.toFixed(1) ?? '--');
        $('#feelsLikeValue').text(`Feels like ${w.feelsLike?.toFixed(1) ?? '--'}°C`);
        $('#weatherDescription').text(w.description || '--');
        $('#weatherIconImg').attr('src', `https://openweathermap.org/img/wn/${w.weatherIcon}@2x.png`);

        $('#humidityVal').text(`${w.humidity}%`);
        $('#windVal').text(`${w.windSpeed?.toFixed(1)} m/s`);
        $('#pressureVal').text(`${w.pressure?.toFixed(0)} hPa`);
        $('#visibilityVal').text(`${(w.visibility / 1000).toFixed(1)} km`);
        $('#cloudsVal').text(`${w.cloudCover}%`);
        $('#windDirVal').text(`${w.windDirection}°`);
        $('#sunriseVal').text(w.sunrise || '--');
        $('#sunsetVal').text(w.sunset || '--');
    }

    // Recommendation
    if (data.recommendation) {
        const r = data.recommendation;
        $('#recommendationBox').show()
            .attr('class', `recommendation-box ${r.verdictClass}`);
        $('#verdictText').text(r.verdict)
            .attr('class', `verdict-text ${r.verdictClass}`);
        $('#overallSummary').text(r.overallSummary);
        $('#heatIndexVal').text(r.heatIndex?.toFixed(1) ?? '--');
        $('#heatIndexCat').text(r.heatIndexCategory || '--');

        const list = $('#dosAndDontsList');
        list.empty();
        (r.dosAndDonts || []).forEach(item => {
            list.append(`<li>${item}</li>`);
        });
    }
}

// ===== City Search =====
function setupSearch() {
    const $input = $('#citySearch');
    const $dropdown = $('#citySearchDropdown');

    $input.on('input', function () {
        const query = $(this).val().toLowerCase().trim();
        if (query.length < 1) {
            $dropdown.removeClass('show');
            return;
        }

        const matches = allDistricts.filter(d =>
            d.name.toLowerCase().includes(query) || d.state.toLowerCase().includes(query)
        ).slice(0, 10);

        if (matches.length === 0) {
            $dropdown.removeClass('show');
            return;
        }

        $dropdown.html(matches.map(d =>
            `<div class="city-search-item" data-id="${d.id}">
                ${d.name} <span class="state">${d.state}</span>
            </div>`
        ).join('')).addClass('show');
    });

    $dropdown.on('click', '.city-search-item', function () {
        const id = parseInt($(this).data('id'));
        const marker = markers.find(m => m.districtData.id === id);
        selectCity(id, marker);
        $input.val('');
        $dropdown.removeClass('show');
    });

    // Close dropdown on outside click
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.city-selector-overlay').length) {
            $dropdown.removeClass('show');
        }
    });
}

// ===== Map Controls =====
function setupControls() {
    // Heatmap toggle
    $('#toggleHeatmap').on('click', function () {
        heatmapVisible = !heatmapVisible;
        $(this).toggleClass('active', heatmapVisible);

        if (heatmapVisible) {
            showHeatmap();
        } else {
            hideHeatmap();
        }
    });

    // Refresh
    $('#refreshMap').on('click', function () {
        $(this).find('i').css('animation', 'spin 0.5s linear');
        loadDistricts();
        if (selectedDistrictId) {
            selectCity(selectedDistrictId, selectedMarker);
        }
        setTimeout(() => $(this).find('i').css('animation', ''), 500);
    });
}

function showHeatmap() {
    const heatData = allDistricts
        .filter(d => d._aqiValue)
        .map(d => [d.latitude, d.longitude, d._aqiValue / 500]);

    if (heatLayer) map.removeLayer(heatLayer);
    heatLayer = L.heatLayer(heatData, {
        radius: 40,
        blur: 30,
        maxZoom: 10,
        gradient: {
            0.0: '#00e400',
            0.2: '#92d050',
            0.4: '#ffff00',
            0.6: '#ff7e00',
            0.8: '#ff0000',
            1.0: '#8f3f97'
        }
    }).addTo(map);
}

function hideHeatmap() {
    if (heatLayer) {
        map.removeLayer(heatLayer);
        heatLayer = null;
    }
}

// ===== Tab Switching =====
function switchTab(btn, tabId) {
    // Update buttons
    $(btn).siblings('.tab-btn').removeClass('active');
    $(btn).addClass('active');

    // Update panels
    $('.tab-panel').removeClass('active');
    $(`#${tabId}`).addClass('active');
}

// ===== Auto-Refresh =====
function refreshData() {
    if (selectedDistrictId) {
        selectCity(selectedDistrictId, selectedMarker);
    }
    // Re-color all visible markers
    coloredMarkerIds.clear();
    lazyColorVisibleMarkers();
}

// ===== Utility Functions =====
function getAQIColor(aqi) {
    if (aqi <= 50) return '#00e400';
    if (aqi <= 100) return '#92d050';
    if (aqi <= 200) return '#ffff00';
    if (aqi <= 300) return '#ff7e00';
    if (aqi <= 400) return '#ff0000';
    return '#8f3f97';
}

function getAQICategoryClass(category) {
    const map = {
        'Good': 'good',
        'Satisfactory': 'satisfactory',
        'Moderate': 'moderate',
        'Poor': 'poor',
        'Very Poor': 'very-poor',
        'Severe': 'severe'
    };
    return map[category] || 'moderate';
}

function getWindDirection(deg) {
    const dirs = ['N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE', 'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW'];
    return dirs[Math.round(deg / 22.5) % 16];
}

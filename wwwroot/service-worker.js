// EnviroWatch — Service Worker
// Offline caching + Push notification handler

const CACHE_NAME = 'envirowatch-v2';
const STATIC_ASSETS = [
    '/css/site.css',
    '/js/map.js',
    '/js/analysis.js'
];

// Install — cache static assets (CSS/JS only, NOT HTML pages)
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

// Activate — clean old caches
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
        ).then(() => self.clients.claim())
    );
});

// Fetch strategy:
//   - API calls → network only
//   - HTML pages (navigate) → network-first (so auth state is always fresh)
//   - Static assets (CSS/JS/images) → cache-first
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    // API calls — always network
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(
            fetch(event.request)
                .catch(() => new Response(JSON.stringify({ error: 'Offline' }), {
                    headers: { 'Content-Type': 'application/json' }
                }))
        );
        return;
    }

    // HTML page navigations — network-first so login/logout state is always current
    if (event.request.mode === 'navigate') {
        event.respondWith(
            fetch(event.request)
                .catch(() => caches.match(event.request))
        );
        return;
    }

    // Static assets (CSS, JS, images) — cache-first
    event.respondWith(
        caches.match(event.request)
            .then(cached => cached || fetch(event.request))
    );
});

// Push notifications
self.addEventListener('push', event => {
    const data = event.data?.json() || { title: 'EnviroWatch Alert', body: 'AQI alert for your subscribed city!' };
    event.waitUntil(
        self.registration.showNotification(data.title, {
            body: data.body,
            icon: '/favicon.ico',
            badge: '/favicon.ico',
            vibrate: [100, 50, 100],
            data: { url: data.url || '/' }
        })
    );
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(
        clients.openWindow(event.notification.data.url)
    );
});

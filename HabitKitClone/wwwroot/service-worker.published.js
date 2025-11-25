// Service Worker for Production
// Caches static assets (CSS, JS, images) but allows server-rendered pages to pass through
// This is optimized for Blazor Server apps which require server connection

const CACHE_NAME = 'habitkit-v1';
const STATIC_ASSETS = [
    // Static assets will be populated from service-worker-assets.js
];

// Install event - cache static assets
self.addEventListener('install', function(event) {
    console.log('[Service Worker] Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME).then(function(cache) {
            // Load assets from the manifest
            return fetch('/service-worker-assets.js')
                .then(response => response.json())
                .then(assets => {
                    const urlsToCache = assets.assets
                        .filter(asset => {
                            // Only cache static assets, not HTML or server-rendered content
                            const url = asset.url;
                            const isStaticAsset = 
                                url.endsWith('.css') ||
                                url.endsWith('.js') ||
                                url.endsWith('.png') ||
                                url.endsWith('.jpg') ||
                                url.endsWith('.jpeg') ||
                                url.endsWith('.gif') ||
                                url.endsWith('.svg') ||
                                url.endsWith('.woff') ||
                                url.endsWith('.woff2') ||
                                url.endsWith('.ttf') ||
                                url.endsWith('.eot') ||
                                url.endsWith('.ico') ||
                                url.includes('/_content/') ||
                                url.includes('/lib/');
                            
                            // Exclude HTML files and server-rendered content
                            const isExcluded = 
                                url.endsWith('.html') ||
                                url.endsWith('/') ||
                                url.includes('/Identity/') ||
                                url.includes('/Account/') ||
                                url.includes('/_framework/') ||
                                url.includes('blazor.web.js');
                            
                            return isStaticAsset && !isExcluded;
                        })
                        .map(asset => asset.url);
                    
                    console.log('[Service Worker] Caching', urlsToCache.length, 'static assets');
                    return cache.addAll(urlsToCache).catch(err => {
                        console.warn('[Service Worker] Failed to cache some assets:', err);
                        // Continue even if some assets fail to cache
                        return Promise.resolve();
                    });
                })
                .catch(err => {
                    console.warn('[Service Worker] Failed to load asset manifest:', err);
                    // Continue without pre-caching if manifest fails
                    return Promise.resolve();
                });
        })
    );
    // Skip waiting to activate immediately
    self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', function(event) {
    console.log('[Service Worker] Activating...');
    event.waitUntil(
        caches.keys().then(function(cacheNames) {
            return Promise.all(
                cacheNames.map(function(cacheName) {
                    if (cacheName !== CACHE_NAME) {
                        console.log('[Service Worker] Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        }).then(function() {
            // Take control of all pages immediately
            return self.clients.claim();
        })
    );
});

// Fetch event - serve from cache for static assets, network for everything else
self.addEventListener('fetch', function(event) {
    const request = event.request;
    const url = new URL(request.url);
    
    // Skip non-GET requests
    if (request.method !== 'GET') {
        event.respondWith(fetch(request));
        return;
    }
    
    // Skip cross-origin requests
    if (url.origin !== location.origin) {
        event.respondWith(fetch(request));
        return;
    }
    
    // For navigation requests (HTML pages), always fetch from network
    // Blazor Server needs live server responses
    if (request.mode === 'navigate') {
        event.respondWith(fetch(request).catch(() => {
            // If network fails, return a basic offline page
            return new Response('You are offline. Please check your connection.', {
                status: 503,
                statusText: 'Service Unavailable',
                headers: { 'Content-Type': 'text/plain' }
            });
        }));
        return;
    }
    
    // For static assets, try cache first, then network
    const isStaticAsset = 
        url.pathname.endsWith('.css') ||
        url.pathname.endsWith('.js') ||
        url.pathname.endsWith('.png') ||
        url.pathname.endsWith('.jpg') ||
        url.pathname.endsWith('.jpeg') ||
        url.pathname.endsWith('.gif') ||
        url.pathname.endsWith('.svg') ||
        url.pathname.endsWith('.woff') ||
        url.pathname.endsWith('.woff2') ||
        url.pathname.endsWith('.ttf') ||
        url.pathname.endsWith('.eot') ||
        url.pathname.endsWith('.ico') ||
        url.pathname.includes('/_content/') ||
        url.pathname.includes('/lib/');
    
    // Exclude server-rendered content and framework files
    const isExcluded = 
        url.pathname.endsWith('.html') ||
        url.pathname.includes('/Identity/') ||
        url.pathname.includes('/Account/') ||
        url.pathname.includes('/_framework/') ||
        url.pathname.includes('blazor.web.js');
    
    if (isStaticAsset && !isExcluded) {
        event.respondWith(
            caches.match(request).then(function(response) {
                // Return cached version if available
                if (response) {
                    return response;
                }
                // Otherwise fetch from network and cache it
                return fetch(request).then(function(response) {
                    // Only cache successful responses
                    if (response.status === 200) {
                        const responseToCache = response.clone();
                        caches.open(CACHE_NAME).then(function(cache) {
                            cache.put(request, responseToCache);
                        });
                    }
                    return response;
                });
            })
        );
    } else {
        // For non-static assets or excluded paths, always fetch from network
        event.respondWith(fetch(request));
    }
});


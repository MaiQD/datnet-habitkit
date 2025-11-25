// Service Worker for Development
// In development, always fetch from network (no caching)
// This ensures developers see changes immediately

self.addEventListener('install', function(event) {
    // Skip waiting to activate immediately
    self.skipWaiting();
});

self.addEventListener('activate', function(event) {
    // Take control of all pages immediately
    event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', function(event) {
    // In development, always fetch from network
    // Don't cache anything to ensure fresh content
    event.respondWith(fetch(event.request));
});


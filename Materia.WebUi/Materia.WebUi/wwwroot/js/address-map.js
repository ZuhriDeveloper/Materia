// Leaflet-based coordinate picker for customer addresses.
// Free stack: OpenStreetMap tiles + Nominatim geocoding (no API key).
// One module instance is shared across pickers, so per-map state is kept in a registry
// keyed by the container element id.

const registry = new Map();
const DEFAULT_CENTER = [-6.200000, 106.816666]; // Jakarta
const DEFAULT_ZOOM = 11;
const PIN_ZOOM = 16;

function round8(v) { return Math.round(v * 1e8) / 1e8; }

function notify(dotnetRef, lat, lng) {
    dotnetRef.invokeMethodAsync('OnPointPicked', round8(lat), round8(lng));
}

export function init(elementId, lat, lng, hasPoint, dotnetRef) {
    const el = document.getElementById(elementId);
    if (!el || typeof L === 'undefined') return;
    if (registry.has(elementId)) dispose(elementId);

    const map = L.map(el).setView(
        hasPoint ? [lat, lng] : DEFAULT_CENTER,
        hasPoint ? PIN_ZOOM : DEFAULT_ZOOM);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap',
    }).addTo(map);

    const state = { map, marker: null };

    state.place = (la, ln, fireCallback) => {
        if (state.marker) {
            state.marker.setLatLng([la, ln]);
        } else {
            state.marker = L.marker([la, ln], { draggable: true }).addTo(map);
            state.marker.on('dragend', () => {
                const p = state.marker.getLatLng();
                notify(dotnetRef, p.lat, p.lng);
            });
        }
        if (fireCallback) notify(dotnetRef, la, ln);
    };

    if (hasPoint) state.place(lat, lng, false);

    map.on('click', (e) => state.place(e.latlng.lat, e.latlng.lng, true));

    registry.set(elementId, state);

    // Containers are often rendered while hidden/animating — fix tile layout afterwards.
    setTimeout(() => map.invalidateSize(), 200);
}

export function setPoint(elementId, lat, lng) {
    const state = registry.get(elementId);
    if (!state) return;
    state.place(lat, lng, false);
    state.map.setView([lat, lng], Math.max(state.map.getZoom(), PIN_ZOOM));
}

// Geocode a free-text query via Nominatim, drop the pin on the first hit, and return
// its display name (or null if nothing matched / the request failed).
export async function search(elementId, query) {
    const state = registry.get(elementId);
    if (!state || !query) return null;

    const url = 'https://nominatim.openstreetmap.org/search'
        + '?format=json&limit=1&addressdetails=0&countrycodes=id'
        + '&q=' + encodeURIComponent(query);
    try {
        const resp = await fetch(url, { headers: { 'Accept-Language': 'id' } });
        if (!resp.ok) return null;
        const data = await resp.json();
        if (!Array.isArray(data) || data.length === 0) return null;

        const lat = parseFloat(data[0].lat);
        const lng = parseFloat(data[0].lon);
        state.place(lat, lng, true);
        state.map.setView([lat, lng], PIN_ZOOM);
        return data[0].display_name || '';
    } catch {
        return null;
    }
}

export function invalidate(elementId) {
    const state = registry.get(elementId);
    if (state) state.map.invalidateSize();
}

export function dispose(elementId) {
    const state = registry.get(elementId);
    if (!state) return;
    try { state.map.remove(); } catch { /* already gone */ }
    registry.delete(elementId);
}

/**
 * printReceipt(isSuratJalan)
 *
 * Dynamically injects the correct @page rule before window.print() so
 * the browser targets the right paper size without a separate stylesheet.
 *
 * - Standard receipt  → 80mm thermal roll, zero margins
 * - Surat Jalan       → 9.5x5.5in continuous form (LX-310 dot matrix),
 *                       8in content band centred via 19mm side margins
 *
 * The injected <style> is appended last in <head> so it always wins the
 * cascade over the static app.css @page rule.
 */
window.printReceipt = function (isSuratJalan) {
    var prev = document.getElementById('_mp_page');
    if (prev) prev.remove();

    var s = document.createElement('style');
    s.id = '_mp_page';
    // The receipt is rendered at its 72mm design width then enlarged 1.5x via
    // `zoom` in CSS (see .receipt-print-only) so it prints correctly at the
    // default 100% scale on this printer. The page box must therefore be the
    // zoomed width — 72mm x 1.5 = 108mm — or the enlarged content gets clipped.
    s.textContent = isSuratJalan
        ? '@page { size: 9.5in 5.5in; margin: 10mm 19mm 4mm 19mm; }'
        : '@page { size: 108mm auto; margin: 0; }' +
          '@media print { html, body { width: 108mm !important; min-width: 0 !important; margin: 0 !important; padding: 0 !important; } }';
    document.head.appendChild(s);

    window.print();
};

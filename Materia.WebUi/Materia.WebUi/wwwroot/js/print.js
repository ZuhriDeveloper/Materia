/**
 * printReceipt(isSuratJalan)
 *
 * Dynamically injects the correct @page rule before window.print() so
 * the browser targets the right paper size without a separate stylesheet.
 *
 * - Standard receipt  → 80mm thermal roll, zero margins
 * - Surat Jalan       → A5 portrait, 12mm/15mm margins
 *
 * The injected <style> is appended last in <head> so it always wins the
 * cascade over the static app.css @page rule.
 */
window.printReceipt = function (isSuratJalan) {
    var prev = document.getElementById('_mp_page');
    if (prev) prev.remove();

    var s = document.createElement('style');
    s.id = '_mp_page';
    // For the thermal receipt we force the page + html/body to the printer's
    // real printable width (72mm — the standard 576-dot area of an "80mm"
    // thermal head at 203 DPI). Matching the physical paper exactly makes the
    // browser print 1:1 at 100% scale instead of shrinking an 80mm layout to
    // fit, which was forcing a manual 130% scale-up.
    s.textContent = isSuratJalan
        ? '@page { size: A5 portrait; margin: 12mm 15mm; }'
        : '@page { size: 72mm auto; margin: 0; }' +
          '@media print { html, body { width: 72mm !important; min-width: 0 !important; margin: 0 !important; padding: 0 !important; } }';
    document.head.appendChild(s);

    window.print();
};

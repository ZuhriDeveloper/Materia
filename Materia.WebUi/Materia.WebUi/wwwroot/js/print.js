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
    s.textContent = isSuratJalan
        ? '@page { size: A5 portrait; margin: 12mm 15mm; }'
        : '@page { size: 80mm auto;    margin: 0; }';
    document.head.appendChild(s);

    window.print();
};

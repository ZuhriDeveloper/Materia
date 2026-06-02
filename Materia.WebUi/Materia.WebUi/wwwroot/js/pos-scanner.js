// Materia PoS — barcode / QR scanner interop.
//
// This is a dependency-free placeholder so the cashier flow works end-to-end today.
// To enable real camera scanning later, replace the body of startScan() with the
// BarcodeDetector API or a library such as @zxing/browser and return the decoded text.

export function startScan() {
    try {
        const code = window.prompt('Pindai atau masukkan barcode / SKU produk:');
        return code ? code.trim() : '';
    } catch {
        return '';
    }
}

export function focusInput(element) {
    if (element && typeof element.focus === 'function') {
        element.focus();
        if (typeof element.select === 'function') {
            element.select();
        }
    }
}

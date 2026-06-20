# Roadmap Fitur Baru — Materia POS

> Urutan sprint berdasarkan prioritas dampak operasional. Tiap sprint bisa dikerjakan secara independen selama sprint sebelumnya selesai.

---

## Ringkasan Sprint

| Sprint | Fitur | Kompleksitas | File Plan |
|--------|-------|-------------|-----------|
| 1 | **Retur Penjualan** — pelanggan kembalikan barang, stok dipulihkan, opsi kurangi piutang | Tinggi (aggregate baru) | [retur-penjualan-plan.md](retur-penjualan-plan.md) |
| 2 | **AP Ledger / Hutang Supplier** — catat pembayaran ke supplier, halaman hutang dengan aging | Menengah (event baru di PO) | [sprint-2-ap-ledger-hutang-supplier.md](sprint-2-ap-ledger-hutang-supplier.md) |
| 3 | **Reorder Point + Dashboard** — alert stok minimum + home page analytics | Rendah (field baru + query) | [sprint-3-reorder-point-dashboard.md](sprint-3-reorder-point-dashboard.md) |
| 4 | **Stock Opname** — sesi hitung fisik stok dengan batch reconciliation | Tinggi (aggregate baru) | [sprint-4-stock-opname.md](sprint-4-stock-opname.md) |
| 5 | **Per-Variant Stock** — stok per warna/varian, wajib pilih warna di POS | Menengah (logika Application) | [sprint-5-per-variant-stock.md](sprint-5-per-variant-stock.md) |
| 6 | **Volume Pricing + Laporan** — harga bertingkat grosir/eceran + 4 laporan baru | Menengah | [sprint-6-volume-pricing-laporan.md](sprint-6-volume-pricing-laporan.md) |

---

## Urutan Dependensi

```
Sprint 1 (Retur Penjualan)
    └── tidak bergantung ke sprint lain

Sprint 2 (Hutang Supplier)
    └── tidak bergantung ke sprint lain

Sprint 3 (Reorder Point + Dashboard)
    └── Dashboard hutang supplier lebih lengkap setelah Sprint 2

Sprint 4 (Stock Opname)
    └── tidak bergantung; lebih bermakna setelah Sprint 3 (reorder alerts)

Sprint 5 (Per-Variant Stock)
    └── SEBAIKNYA setelah Sprint 4 (opname digunakan untuk migrasi stok variant)

Sprint 6 (Volume Pricing + Laporan)
    └── Dashboard Sprint 3 harus ada dulu untuk laporan net position
```

---

## Gambaran Fitur per Sprint

### Sprint 1 — Retur Penjualan
**Masalah:** Tidak ada cara mencatat barang yang dikembalikan pelanggan.
- Aggregate baru `SaleReturn` dengan event `SaleReturnRecorded`
- Stok dikembalikan otomatis
- Resolusi: refund tunai atau kurangi piutang pelanggan
- UI: tombol "Retur" di SaleDetail + dialog form

### Sprint 2 — AP Ledger / Hutang Supplier
**Masalah:** Tenor PO sudah ada, tapi tidak bisa mencatat pembayaran ke supplier.
- Event baru `PurchaseOrderPaymentRecorded` di PO aggregate
- Halaman "Hutang Supplier" mirip halaman Piutang Pelanggan
- Badge merah untuk yang sudah jatuh tempo

### Sprint 3 — Reorder Point + Dashboard
**Masalah:** Stok habis baru ketahuan saat pelanggan mau beli. Home page kosong.
- Field `MinimumStock` + `ReorderQty` di Product (event `ProductMinimumStockSet`)
- Widget di Home: stok kritis, omzet hari ini, piutang/hutang jatuh tempo
- Grafik tren penjualan 7 hari

### Sprint 4 — Stock Opname
**Masalah:** Tidak ada sesi hitung fisik stok yang terstruktur.
- Aggregate baru `StockOpname`: Draft → InProgress → Completed
- Snapshot qty sistem saat Start, input qty aktual per produk, batch adjustment saat Complete
- Laporan hasil: selisih positif/negatif per produk + nilai total selisih

### Sprint 5 — Per-Variant Stock
**Masalah:** Cat merah dan cat biru dihitung dalam satu pool stok.
- Infrastruktur sudah siap (`StockReadModel.VariantId?`) — perlu aktivasi di Application layer
- Inisialisasi variant stock otomatis saat warna ditambahkan
- POS wajib pilih warna untuk produk bervariant
- Laporan stok menampilkan breakdown per warna

### Sprint 6 — Volume Pricing + Laporan
**Masalah:** Harga flat saja, tidak bisa bedakan eceran vs grosir. Laporan bisnis terbatas.
- `PriceTier` di Product: beli ≥ N → harga X
- Auto-apply tier di POS berdasarkan qty
- 4 laporan baru: per kasir, produk terlaris, stok mati, net posisi piutang-hutang

---

## Catatan Arsitektur

Semua sprint mengikuti pola yang sudah ada di codebase:
- **Domain**: aggregate → event → Apply (event sourcing)
- **Tests**: TDD — tulis test sebelum handler
- **Application**: command + FluentValidation validator + handler
- **Infrastructure**: read model + projection + repository + EF migration
- **API**: controller endpoint
- **WebUi**: Blazor page/component dengan MudBlazor

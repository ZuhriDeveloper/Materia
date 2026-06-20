# Sprint 5 — Per-Variant Stock (Increment 2)

> **Status:** Siap dieksekusi setelah Sprint 4 selesai.

---

## Context

Saat ini semua warna/varian sebuah produk berbagi satu stock record (`Stock.VariantId == null`). Cat merah 5L dan cat biru 5L dihitung sebagai satu pool stok yang sama. Ini sudah cukup untuk "Increment 1", tapi toko bangunan perlu tahu stok per warna.

Infrastruktur sudah siap sebagian: `Stock` aggregate sudah punya field `VariantId?`, `StockReadModel` punya `VariantId?`, dan `StockDeductionService` sudah menerima `variantId`. Yang belum ada adalah:
1. Saat produk punya color variants, transaksi **belum otomatis** menggunakan variant-level stock.
2. Saat PO diterima untuk produk dengan variants, stok perlu dimasukkan ke variant-level.

---

## Keputusan Desain

| Keputusan | Pilihan | Alasan |
|---|---|---|
| Inisialisasi variant stock | Otomatis saat `ProductColorVariantAdded` — handler Application membuat `Stock.Initialize(variantId)` | Tidak perlu admin buat stock manual; DRY |
| Penjualan dengan variant | `FinalizeSaleCommandHandler` sudah kirim `variantId` ke `DeductAsync` — tinggal pastikan variant stock ada | Kode sudah siap, tinggal diaktifkan |
| PO receive dengan variant | `ReceivePurchaseOrderCommand` perlu tambah `VariantId?` per line | Kasir/admin pilih variant saat terima barang |
| Fallback ke product-level stock | Tidak ada fallback — jika produk punya variants, wajib pilih variant | Menghindari ambiguitas stock |
| Migrasi stok lama | Untuk produk yang sudah punya variant + stock product-level: buat migration script atau opname manual | Tidak ada auto-migration domain — ini keputusan operasional |

---

## Fase 1 — Domain

Domain tidak perlu perubahan besar — `Stock` aggregate sudah support `VariantId`. Yang berubah adalah **aturan bisnis di Application layer**.

### 1.1 Tidak ada perubahan domain aggregate

`Stock`, `Product`, `Sale`, `PurchaseOrder` tidak perlu event baru untuk fitur ini.

### 1.2 Tambah constraint di Application layer

Di `FinalizeSaleCommandHandler`:

```csharp
// Sebelumnya: tidak ada validasi variant
// Setelah: jika produk punya color variants aktif, wajib ada variantId di item
private static void ValidateVariantRequired(SaleItemCommand item, ProductDto product)
{
    var hasActiveVariants = product.ColorVariants.Any(v => v.IsActive);
    if (hasActiveVariants && item.VariantId is null)
        throw new DomainException(
            $"Produk '{product.Name}' memiliki varian warna — pilih warna yang dijual.");
}
```

---

## Fase 2 — Tests

**File baru:** `Materia.Tests/Sales/FinalizeSaleVariantStockTests.cs`

- ✅ Produk dengan variants → wajib ada VariantId di item, kalau tidak → DomainException
- ✅ Produk tanpa variants → VariantId null diperbolehkan
- ✅ Stock deduction menggunakan variant stock (integrasi test dengan real DB)

**Update:** `Materia.Tests/Purchasing/ReceivePurchaseOrderTests.cs`

- ✅ Receive PO untuk produk dengan variant → stok variant yang berkurang/bertambah
- ✅ Receive tanpa variantId tapi produk punya variants → DomainException

---

## Fase 3 — Application — Inisialisasi Variant Stock

### 3.1 Handler baru: InitializeVariantStock

Saat `ProductColorVariantAdded` diproses (setelah `AddColorVariant` command), tambah handler di Application yang:
1. Buat `Stock.Initialize(productId, baseUnit, createdBy, variantId)`
2. Save ke StockRepository

**Opsi A (rekomendasi):** Tambahkan sebagai efek samping di `AddColorVariantCommandHandler`:

```csharp
// Di AddColorVariantCommandHandler, setelah save product:
var variantId = VariantId.From(newVariantId);
var stock = Stock.Initialize(ProductId.From(command.ProductId), product.BaseUnit, command.UpdatedBy, variantId);
await stockRepository.SaveAsync(stock, ct);
```

**Opsi B:** Event handler yang listen ke `ProductColorVariantAdded`. Lebih decoupled tapi lebih complex untuk codebase seukuran ini. Pilih Opsi A.

### 3.2 Update ReceivePurchaseOrderCommand

**File:** `ReceivePurchaseOrderCommand.cs`

Tambah `VariantId?` ke DTO line:

```csharp
public sealed record ReceiveLineDto(
    Guid  ProductId,
    Guid? VariantId,   // <-- BARU
    decimal ReceivedQty);
```

**Update handler** `ReceivePurchaseOrderCommandHandler`: gunakan `variantId` saat reconcile stock.

### 3.3 Update FinalizeSaleCommandHandler

Tambah validasi variant (lihat Fase 1 di atas).

### 3.4 Update Stock Report Query

`GetStockAsync` atau `GetLowStockAsync` — pastikan query bisa filter per variant dan menampilkan stok per warna.

---

## Fase 4 — Infrastructure

Tidak ada perubahan read model besar — `StockReadModel` sudah punya `VariantId?`. Yang perlu diperbarui:

### 4.1 StockQueryRepository

Tambah atau update method:

```csharp
Task<IReadOnlyList<StockReadModel>> GetByProductAsync(
    Guid productId, CancellationToken ct = default);
// → Mengembalikan product-level stock (VariantId == null) ATAU semua variant stocks
```

### 4.2 Update StockReport Query

Pastikan laporan stok menampilkan breakdown per variant jika ada:

```
Produk: Cat Tembok Premium
  ├─ Merah ............ 25 kaleng
  ├─ Putih ............ 40 kaleng
  └─ Biru ............. 10 kaleng
```

### 4.3 EF Migration

Tidak ada perubahan schema. Hanya perlu pastikan existing data `VariantId` dihandle benar.

---

## Fase 5 — API

### Update yang diperlukan

```
POST /api/purchase-orders/{id}/receive
Body sekarang: { lines: [{ productId, receivedQty }] }
Body setelah:  { lines: [{ productId, variantId?, receivedQty }] }
```

```
GET /api/products/{id}/stock
Response sekarang: { quantity, unit, averageCost }
Response setelah:  {
  productLevel: { quantity, unit, averageCost },  // null jika semua pakai variant
  variants: [
    { variantId, colorName, quantity, unit, averageCost }
  ]
}
```

```
GET /api/products/low-stock
→ Sudah mendukung VariantId dari Sprint 3 — pastikan breakdown per variant muncul
```

---

## Fase 6 — WebUi

### 6.1 POS — Pilih Warna

**File:** `Pos.razor`

Saat menambah item ke cart dan produk punya variants aktif:
- Tampilkan dropdown/selector warna SEBELUM add ke cart
- Tampilkan stok per warna di sebelah pilihan warna

### 6.2 Receive PO — Pilih Varian

**File:** `PurchaseOrderDetail.razor`

Di form "Terima Barang", untuk produk dengan variants:
- Tampilkan pilihan warna/variant per baris
- Qty terima bisa dipecah per warna (misal: terima 10 merah + 5 biru dari 1 PO line)

> **Note:** Jika PO dibuat tanpa specify variant (produk punya variants), UI perlu minta konfirmasi split saat menerima.

### 6.3 Stock Report — Breakdown Variant

**File:** `StockReport.razor`

- Expandable row: klik produk → expand menampilkan stok per warna
- Filter: tampilkan hanya produk dengan variants / hanya produk tanpa variants / semua

---

## Checklist Eksekusi

```
[ ] Fase 2 — Tests (tulis dulu)
    [ ] FinalizeSaleVariantStockTests
    [ ] Update ReceivePurchaseOrderTests

[ ] Fase 3 — Application
    [ ] Tambah init variant stock di AddColorVariantCommandHandler
    [ ] Tambah VariantId? ke ReceiveLineDto
    [ ] Update ReceivePurchaseOrderCommandHandler
    [ ] Tambah validasi variant di FinalizeSaleCommandHandler

[ ] Fase 4 — Infrastructure
    [ ] Update GetByProductAsync di StockQueryRepository
    [ ] Update StockReport query untuk breakdown variant

[ ] Fase 5 — API
    [ ] Update POST receive endpoint (tambah variantId)
    [ ] Update GET stock endpoint (breakdown per variant)

[ ] Fase 6 — WebUi
    [ ] Selector warna di POS sebelum add to cart
    [ ] Split receive per warna di PurchaseOrderDetail
    [ ] Expandable variant rows di StockReport
```

---

## Catatan Migrasi Data

Produk yang **sudah punya color variants** sebelum sprint ini dieksekusi akan:
- Punya product-level stock (VariantId == null) = total semua warna
- Tidak punya variant-level stock per warna

**Rekomendasi:** Jalankan **Stock Opname** (Sprint 4) setelah Sprint 5 selesai untuk produk-produk ini. Saat opname, stok per warna bisa diisi secara manual sehingga tersinkron. Product-level stock yang lama tetap ada tapi tidak dipakai lagi untuk produk yang sudah punya variants.

Alternatif: buat admin script sekali pakai yang membagi product-level stock ke variant-level secara proporsional — tapi ini berisiko salah distribusi, lebih aman opname manual.

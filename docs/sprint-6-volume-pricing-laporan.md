# Sprint 6 — Volume Pricing & Laporan Lengkap

> **Status:** Siap dieksekusi setelah Sprint 5 selesai.

---

## Context

Sprint terakhir di roadmap ini menambahkan dua fitur yang meningkatkan kemampuan analitik dan fleksibilitas harga:

1. **Volume Pricing (Harga Bertingkat)** — harga otomatis turun saat pelanggan beli dalam jumlah besar. Umum di toko bangunan: eceran vs grosir.
2. **Laporan Lengkap** — empat laporan tambahan di luar P&L dan cash flow yang sudah ada: penjualan per kasir, produk terlaris, stok mati (slow-moving), dan rekap posisi net piutang-hutang.

---

## Bagian A — Volume Pricing (Harga Bertingkat)

### A.1 Keputusan Desain

| Keputusan | Pilihan | Alasan |
|---|---|---|
| Scope tier | Per produk, berlaku untuk semua pelanggan | Tidak ada tiering per pelanggan (terlalu kompleks untuk fase ini) |
| Tier berbasis unit apa | Unit dasar (BaseUnit) produk | Konsisten dengan cara stok dihitung; menghindari ambiguitas multi-unit |
| Berapa tier maksimum | Tidak dibatasi domain, tapi UI batasi 5 tier | Lebih dari 5 tier jarang terjadi di praktik |
| Override vs tier | Tier override `SalePrice` default produk | Tier hanya berlaku saat qty memenuhi; di bawah tier terendah tetap pakai `SalePrice` |
| Snapshot harga di sale | Ya — `unitPrice` di `SaleItemAdded` sudah di-snapshot saat item ditambahkan | Tidak ada perubahan pada Sale aggregate |

### A.2 Domain — Events

**File baru:** `Materia.Domain/Inventory/Events/ProductPriceTierAdded.cs`

```csharp
public record ProductPriceTierAdded(
    ProductId ProductId,
    Guid      TierId,
    decimal   MinQty,
    decimal   UnitPrice,
    string    UpdatedBy,
    DateTime  OccurredAt) : IDomainEvent;
```

**File baru:** `Events/ProductPriceTierRemoved.cs`

```csharp
public record ProductPriceTierRemoved(
    ProductId ProductId,
    Guid      TierId,
    string    UpdatedBy,
    DateTime  OccurredAt) : IDomainEvent;
```

### A.3 Value Object & Entity

**File baru:** `Materia.Domain/Inventory/PriceTier.cs`

```csharp
namespace Materia.Domain.Inventory;

public sealed class PriceTier
{
    public Guid    Id        { get; }
    public decimal MinQty    { get; }   // qty dalam BaseUnit
    public decimal UnitPrice { get; }   // harga per BaseUnit saat tier ini berlaku

    internal PriceTier(Guid id, decimal minQty, decimal unitPrice)
    {
        Id = id; MinQty = minQty; UnitPrice = unitPrice;
    }
}
```

### A.4 Domain — Commands di Product

Tambah ke `Product.cs`:

```csharp
private readonly List<PriceTier> _priceTiers = [];
public IReadOnlyList<PriceTier> PriceTiers => _priceTiers.AsReadOnly();

public Guid AddPriceTier(decimal minQty, decimal unitPrice, string updatedBy)
{
    if (!IsActive)
        throw new DomainException("Tidak dapat menambah tier harga pada produk tidak aktif.");
    if (minQty <= 0)
        throw new DomainException("Minimum qty harus lebih dari nol.");
    if (unitPrice < 0)
        throw new DomainException("Harga per unit tidak boleh negatif.");
    if (_priceTiers.Any(t => t.MinQty == minQty))
        throw new DomainException($"Tier dengan minimum {minQty} sudah ada.");

    var tierId = Guid.NewGuid();
    Raise(new ProductPriceTierAdded(Id, tierId, minQty, unitPrice, updatedBy, DateTime.UtcNow));
    return tierId;
}

public void RemovePriceTier(Guid tierId, string updatedBy)
{
    if (_priceTiers.All(t => t.Id != tierId)) return;  // idempotent
    Raise(new ProductPriceTierRemoved(Id, tierId, updatedBy, DateTime.UtcNow));
}

/// <summary>Harga yang berlaku untuk qty tertentu (dalam BaseUnit). Null jika tidak ada tier yang berlaku.</summary>
public decimal? GetTierPrice(decimal qtyInBaseUnit)
{
    return _priceTiers
        .Where(t => qtyInBaseUnit >= t.MinQty)
        .OrderByDescending(t => t.MinQty)
        .FirstOrDefault()?.UnitPrice;
}
```

Apply di `Product.Apply()`:

```csharp
case ProductPriceTierAdded e:
    _priceTiers.Add(new PriceTier(e.TierId, e.MinQty, e.UnitPrice));
    break;

case ProductPriceTierRemoved e:
    _priceTiers.RemoveAll(t => t.Id == e.TierId);
    break;
```

### A.5 Integrasi ke POS

**File:** `Materia.Application/Commands/Sales/FinalizeSale/FinalizeSaleCommandHandler.cs`

Saat menambah item ke sale, setelah resolve `quantityInBaseUnit`, cek apakah ada tier yang berlaku:

```csharp
// Setelah resolve quantityInBaseUnit:
var effectiveUnitPrice = item.UnitPrice;  // user-input price (override manual)

// Jika tidak ada override harga manual dari user, cek tier
if (item.UnitPrice == product.SalePrice || item.UnitPrice == 0)
{
    var tierPrice = product.GetTierPriceForQty(quantityInBaseUnit);
    effectiveUnitPrice = tierPrice ?? product.SalePrice;
}
```

> **Catatan:** `GetTierPrice` harus tersedia di `ProductDto` (DTO yang dipakai di Application layer). Tambahkan `PriceTiers` ke `ProductDto`.

### A.6 Infrastructure

Tambah ke `ProductReadModel`:

```csharp
/// <summary>JSON array of { id, minQty, unitPrice } objects.</summary>
public string PriceTiersJson { get; set; } = "[]";
```

Update `ProductProjection` untuk handle `ProductPriceTierAdded` dan `ProductPriceTierRemoved`.

### A.7 Tests

**File baru:** `Materia.Tests/Inventory/ProductPriceTierTests.cs`

- ✅ `AddPriceTier()` sukses → event emitted, tier terdaftar
- ✅ MinQty duplikat → DomainException
- ✅ MinQty ≤ 0 → DomainException
- ✅ `GetTierPrice(30)` dengan tier [10→Rp1000, 25→Rp900] → Rp900
- ✅ `GetTierPrice(5)` tanpa tier yang match → null (fallback ke SalePrice)
- ✅ `RemovePriceTier()` → tier dihapus
- ✅ Produk tidak aktif → DomainException

### A.8 API

```
POST /api/products/{id}/price-tiers
Body: { "minQty": 10, "unitPrice": 85000 }
Authorization: Admin

DELETE /api/products/{id}/price-tiers/{tierId}
Authorization: Admin
```

`GET /api/products/{id}` sudah include `PriceTiersJson` — cukup tambahkan deserialize ke DTO.

### A.9 WebUi

Di `ProductDetail.razor`, tambah section "Harga Bertingkat":

```
Qty ≥ 1   →  Rp 95.000  (harga eceran / SalePrice default)
Qty ≥ 10  →  Rp 90.000  [hapus]
Qty ≥ 25  →  Rp 85.000  [hapus]
[+ Tambah Tier Harga]
```

---

## Bagian B — Laporan Lengkap

Semua laporan di bawah adalah **query-only** ke read models yang sudah ada. Tidak ada domain event baru.

### B.1 Laporan Penjualan per Kasir

**Endpoint:** `GET /api/reports/sales-by-cashier`

Query params: `from`, `to` (tanggal)

Query: GROUP BY `ServedBy` di `SaleReadModel` WHERE `Status IN (Paid, PartiallyPaid)` AND `CreatedAt BETWEEN from AND to`

**Response:**

```json
[
  {
    "cashierName": "Budi",
    "transactionCount": 45,
    "totalRevenue": 12500000,
    "totalDiscount": 250000,
    "averageTransactionValue": 277777
  }
]
```

**WebUi:** `Pages/Reports/SalesByCashier.razor`

Tabel + grafik batang per kasir. Filter tanggal.

### B.2 Laporan Produk Terlaris

**Endpoint:** `GET /api/reports/top-products`

Query params: `from`, `to`, `limit` (default 20)

Query: JOIN `SaleItemReadModel` × `SaleReadModel` WHERE status settled, GROUP BY `ProductId`, ORDER BY `SUM(Subtotal) DESC`

**Response:**

```json
[
  {
    "productId": "...",
    "productName": "Semen Tiga Roda 50kg",
    "totalQtySold": 150,
    "totalRevenue": 18750000,
    "rank": 1
  }
]
```

**WebUi:** `Pages/Reports/TopProducts.razor`

Tabel + grafik (pie atau horizontal bar). Filter tanggal + limit.

### B.3 Laporan Stok Mati (Slow-Moving)

**Endpoint:** `GET /api/reports/slow-moving-stock`

Query params: `daysSinceLastSale` (default 30)

Query: Produk aktif yang tidak muncul di `SaleItemReadModel` dalam X hari terakhir. JOIN dengan `StockReadModel` untuk qty saat ini.

**Response:**

```json
[
  {
    "productId": "...",
    "productName": "Cat Genteng Spesial",
    "currentStock": 25,
    "lastSoldAt": "2026-04-10",  // null jika belum pernah terjual
    "daysSinceLastSale": 72,
    "stockValue": 3125000   // qty × averageCost
  }
]
```

**WebUi:** `Pages/Reports/SlowMovingStock.razor`

Tabel dengan input "tidak terjual dalam X hari". Kolom nilai stok mati total di footer.

### B.4 Rekap Net Posisi (Piutang − Hutang)

**Endpoint:** `GET /api/reports/net-position`

Query:

```json
{
  "totalReceivables": 8500000,    // SUM(OutstandingDebt) dari CustomerReadModel
  "totalPayables": 5200000,       // SUM(OutstandingAmount) dari PurchaseOrderReadModel
  "netPosition": 3300000,         // totalReceivables - totalPayables
  "overdueReceivables": 1200000,  // piutang dari bon > 30 hari
  "overduePayables": 2000000,     // hutang supplier jatuh tempo
  "topDebtors": [...],            // 5 pelanggan dengan piutang terbesar
  "topCreditors": [...]           // 5 supplier dengan hutang terbesar
}
```

**WebUi:** `Pages/Reports/NetPosition.razor`

Summary kartu + dua tabel (top debtors dan top creditors).

---

## Checklist Eksekusi

```
[ ] Bagian A — Volume Pricing
    [ ] ProductPriceTierAdded + ProductPriceTierRemoved events
    [ ] PriceTier value object
    [ ] Product.AddPriceTier() + RemovePriceTier() + GetTierPrice() + Apply
    [ ] ProductPriceTierTests
    [ ] AddPriceTierCommand + Validator + Handler
    [ ] RemovePriceTierCommand + Handler
    [ ] ProductReadModel.PriceTiersJson
    [ ] Update ProductProjection
    [ ] Update ProductDto dengan PriceTiers
    [ ] Integrasi tier di FinalizeSaleCommandHandler
    [ ] EF migration (tidak ada kolom baru, hanya JSON field di ProductReadModel)
    [ ] POST /api/products/{id}/price-tiers endpoint
    [ ] DELETE /api/products/{id}/price-tiers/{tierId} endpoint
    [ ] UI harga bertingkat di ProductDetail.razor

[ ] Bagian B — Laporan
    [ ] GET /api/reports/sales-by-cashier + SalesByCashier.razor
    [ ] GET /api/reports/top-products + TopProducts.razor
    [ ] GET /api/reports/slow-moving-stock + SlowMovingStock.razor
    [ ] GET /api/reports/net-position + NetPosition.razor
    [ ] Nav menu "Laporan" dengan submenu keempat laporan
```

---

## Aturan Bisnis Penting — Volume Pricing

1. Tier berlaku berdasarkan **qty dalam BaseUnit** — bukan qty dalam unit yang diinput kasir.
2. Jika ada lebih dari satu tier yang berlaku, pilih tier dengan **MinQty tertinggi** (tier grosir mengalahkan tier eceran).
3. Tier tidak override harga yang di-input manual oleh kasir — tier hanya sebagai default.
4. Tier tidak berlaku per warna/variant — berlaku untuk produk secara keseluruhan.
5. Tiers disimpan sebagai JSON di `ProductReadModel` (sama seperti `UnitConversionsJson`) — tidak perlu tabel terpisah.

# Sprint 3 — Reorder Point & Dashboard Analytics

> **Status:** Siap dieksekusi setelah Sprint 2 selesai.

---

## Context

Dua fitur kecil yang saling melengkapi dan bisa dikerjakan dalam satu sprint:

1. **Reorder Point** — tiap produk bisa punya stok minimum; saat stok di bawah ambang, muncul alert.
2. **Dashboard Analytics** — home page yang saat ini kosong diisi snapshot bisnis harian (omzet, stok kritis, piutang jatuh tempo, hutang supplier jatuh tempo).

Keduanya **hanya baca** — tidak ada aggregate baru, cukup field baru di `Product` + query baru di read models yang sudah ada.

---

## Bagian A — Reorder Point

### A.1 Domain — Event Baru

**File baru:** `Materia.Backend/Materia.Domain/Inventory/Events/ProductMinimumStockSet.cs`

```csharp
using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductMinimumStockSet(
    ProductId ProductId,
    decimal   MinimumStock,
    decimal   ReorderQty,
    string    UpdatedBy,
    DateTime  OccurredAt) : IDomainEvent;
```

### A.2 Domain — Command di Product

Tambah property dan method ke `Product.cs`:

```csharp
public decimal MinimumStock { get; private set; }
public decimal ReorderQty   { get; private set; }

public void SetMinimumStock(decimal minimumStock, decimal reorderQty, string updatedBy)
{
    if (!IsActive)
        throw new DomainException("Tidak dapat mengatur stok minimum produk yang tidak aktif.");
    if (minimumStock < 0)
        throw new DomainException("Stok minimum tidak boleh negatif.");
    if (reorderQty < 0)
        throw new DomainException("Kuantitas reorder tidak boleh negatif.");

    Raise(new ProductMinimumStockSet(Id, minimumStock, reorderQty, updatedBy, DateTime.UtcNow));
}
```

Apply di `Product.Apply()`:

```csharp
case ProductMinimumStockSet e:
    MinimumStock = e.MinimumStock;
    ReorderQty   = e.ReorderQty;
    break;
```

### A.3 Tests

**File baru:** `Materia.Tests/Inventory/ProductMinimumStockTests.cs`

- ✅ `SetMinimumStock()` sukses → event emitted, property diupdate
- ✅ Produk tidak aktif → `DomainException`
- ✅ `minimumStock < 0` → `DomainException`
- ✅ `reorderQty < 0` → `DomainException`
- ✅ Set ke 0 valid (artinya tidak ada minimum)

### A.4 Application

**File baru:** `Commands/Inventory/SetMinimumStock/SetMinimumStockCommand.cs`

```csharp
public sealed record SetMinimumStockCommand(
    Guid    ProductId,
    decimal MinimumStock,
    decimal ReorderQty,
    string  UpdatedBy);
```

Handler: load Product → `product.SetMinimumStock(...)` → save.

Validator: `ProductId != Empty`, `MinimumStock >= 0`, `ReorderQty >= 0`.

### A.5 Infrastructure — ProductReadModel

Tambah kolom ke `ProductReadModel.cs`:

```csharp
public decimal MinimumStock { get; set; }
public decimal ReorderQty   { get; set; }
```

Update projection untuk handle `ProductMinimumStockSet`.

### A.6 Query — Produk di Bawah Minimum

Tambah ke `IProductQueryRepository`:

```csharp
/// <summary>Produk dengan stok saat ini di bawah MinimumStock (MinimumStock > 0).</summary>
Task<IReadOnlyList<LowStockProductDto>> GetLowStockAsync(CancellationToken ct = default);
```

**DTO:**

```csharp
public record LowStockProductDto(
    Guid    ProductId,
    string  ProductName,
    string  BaseUnit,
    decimal CurrentStock,
    decimal MinimumStock,
    decimal ReorderQty,
    decimal Shortage);   // MinimumStock - CurrentStock
```

Query implementasi: JOIN `ProductReadModel` × `StockReadModel` WHERE `StockReadModel.Quantity < ProductReadModel.MinimumStock AND ProductReadModel.MinimumStock > 0`.

### A.7 API

```
GET /api/products/low-stock
→ List produk dengan stok di bawah minimum
Authorization: Admin, Gudang

PUT /api/products/{id}/minimum-stock
Body: { "minimumStock": 10, "reorderQty": 50 }
Authorization: Admin
```

### A.8 WebUi — Di ProductDetail

**File:** `ProductDetail.razor`

Tambah section "Pengaturan Stok Minimum":
- Input `MinimumStock` dan `ReorderQty`
- Tombol "Simpan"

---

## Bagian B — Dashboard Analytics

Dashboard dibuat **sepenuhnya dari read models** yang sudah ada. Tidak perlu aggregate baru. Cukup satu endpoint aggregator di API + satu halaman Blazor.

### B.1 API — Dashboard Endpoint

**File baru:** `Controllers/DashboardController.cs`

```
GET /api/dashboard
Authorization: Admin, Cashier (read-only)
```

**Response:**

```json
{
  "today": {
    "totalRevenue": 5250000,
    "transactionCount": 23,
    "averageTransactionValue": 228260,
    "cashRevenue": 4000000,
    "transferRevenue": 1250000
  },
  "lowStockCount": 7,
  "overdueReceivables": {
    "customerCount": 3,
    "totalAmount": 1250000
  },
  "overduePayables": {
    "poCount": 2,
    "totalAmount": 3000000
  },
  "topProducts": [
    { "productId": "...", "name": "Semen Tiga Roda", "totalQty": 50, "totalRevenue": 2500000 }
  ],
  "revenueByDay": [
    { "date": "2026-06-15", "revenue": 1200000 },
    ...  // 7 hari terakhir
  ]
}
```

Semua data diambil dari query ke:
- `SaleReadModel` → today revenue, transaction count, top products, daily trend
- `StockReadModel + ProductReadModel` → low stock count (dari query A.6)
- `CustomerReadModel` → overdue receivables (customer dengan `OutstandingDebt > 0` dan tanggal bon terlama > 30 hari)
- `PurchaseOrderReadModel` → overdue payables (`PaymentDueDate < today` dan `OutstandingAmount > 0`)

### B.2 WebUi — Home Page

**File:** `Materia.WebUi/Components/Pages/Home.razor`

Ganti konten home page dengan grid kartu:

```
┌──────────────────┬──────────────────┐
│  Omzet Hari Ini  │  Transaksi Hari  │
│   Rp 5.250.000   │       23         │
├──────────────────┼──────────────────┤
│  Stok Kritis     │  Piutang Jatuh   │
│   7 produk  →    │  Tempo: 3 cust.  │
├──────────────────┼──────────────────┤
│  Hutang Supplier │  Top Produk      │
│  2 PO overdue →  │  (tabel kecil)   │
├──────────────────┴──────────────────┤
│    Grafik Tren Penjualan 7 Hari     │
└─────────────────────────────────────┘
```

Komponen yang dipakai: `MudCard`, `MudChart` (line chart), `MudTable`.

Kartu "Stok Kritis" dan "Piutang Jatuh Tempo" bisa diklik → navigasi ke halaman terkait.

---

## Checklist Eksekusi

```
[ ] Bagian A — Reorder Point
    [ ] ProductMinimumStockSet event
    [ ] Product.SetMinimumStock() + Apply
    [ ] ProductMinimumStockTests
    [ ] SetMinimumStockCommand + Validator + Handler
    [ ] ProductReadModel kolom baru (MinimumStock, ReorderQty)
    [ ] Update ProductProjection
    [ ] GetLowStockAsync query
    [ ] EF Core migration
    [ ] PUT /api/products/{id}/minimum-stock endpoint
    [ ] GET /api/products/low-stock endpoint
    [ ] Input minimum stock di ProductDetail.razor

[ ] Bagian B — Dashboard
    [ ] DashboardController + GET /api/dashboard
    [ ] Home.razor redesign (kartu + grafik)
    [ ] Kartu Omzet, Transaksi, Stok Kritis, Piutang, Hutang, Top Produk
    [ ] Grafik tren 7 hari (MudChart)
```

# Plan Implementasi — Retur Penjualan (Sales Return)

> **Status:** Siap dieksekusi. Ikuti urutan fase; tiap fase harus hijau sebelum lanjut ke berikutnya.

---

## Context

Saat ini tidak ada mekanisme untuk mencatat barang yang dikembalikan pelanggan setelah penjualan selesai (`Paid` / `PartiallyPaid`). Staff terpaksa workaround dengan stock adjustment manual dan nota fiktif — ini mengacaukan laporan stok dan P&L.

Fitur ini menambahkan aggregate baru `SaleReturn` di domain `Sales`, beserta restorasi stok otomatis dan opsi pengurangan piutang pelanggan (untuk penjualan kredit).

---

## Keputusan Desain

| Keputusan | Pilihan | Alasan |
|---|---|---|
| Aggregate terpisah vs command di Sale | **Aggregate terpisah `SaleReturn`** | Satu penjualan bisa ada beberapa retur parsial; lifecycle retur independen dari lifecycle sale |
| Restorasi stok | Positive `AdjustStock` via `IStockDeductionService` negatif → gunakan `AdjustStockCommand` langsung dengan qty positif | Konsisten dengan pola yang sudah ada |
| Resolusi retur piutang | Method baru `Customer.ReduceDebtForReturn()` + event baru | Tidak merusak semantik `RecordRepayment` (pelunasan ≠ retur) |
| Refund tunai | Hanya dicatat, tidak otomatis mengurangi kas/change fund | Proses refund tunai manual oleh kasir |
| Validasi qty dikembalikan | Di Application layer: qty retur ≤ qty original per item per sale | Domain tidak menyimpan riwayat retur satu penjualan — validasi di handler via Sale read model |

---

## Fase 1 — Domain

### 1.1 Enum baru

**File baru:** `Materia.Backend/Materia.Domain/Sales/SaleReturnEnums.cs`

```csharp
namespace Materia.Domain.Sales;

// NOTE: serialize by ordinal — only ever APPEND new values, never reorder.
public enum ReturnResolution { CashRefund, DebtReduction }
```

### 1.2 Typed ID

**File baru:** `Materia.Backend/Materia.Domain/Sales/SaleReturnId.cs`

```csharp
namespace Materia.Domain.Sales;

public readonly record struct SaleReturnId(Guid Value)
{
    public static SaleReturnId New()  => new(Guid.NewGuid());
    public static SaleReturnId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
```

### 1.3 Domain Event

**File baru:** `Materia.Backend/Materia.Domain/Sales/Events/SaleReturnRecorded.cs`

```csharp
using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleReturnRecorded(
    SaleReturnId   ReturnId,
    SaleId         OriginalSaleId,
    string         OriginalReferenceNo,
    IReadOnlyList<SaleReturnLineData> Lines,
    decimal        TotalRefundAmount,
    ReturnResolution Resolution,
    string         Reason,
    string         ReturnedBy,
    DateTime       OccurredAt) : IDomainEvent;

public record SaleReturnLineData(
    Guid    ProductId,
    string  ProductName,
    Guid?   VariantId,
    string? ColorName,
    string  UnitName,
    decimal Quantity,
    decimal QuantityInBaseUnit,
    decimal UnitPrice);
```

### 1.4 Aggregate

**File baru:** `Materia.Backend/Materia.Domain/Sales/SaleReturn.cs`

```csharp
using Materia.Domain.Common;
using Materia.Domain.Sales.Events;

namespace Materia.Domain.Sales;

public sealed class SaleReturn : AggregateRoot<SaleReturnId>
{
    private readonly List<SaleReturnLine> _lines = [];

    public SaleId           OriginalSaleId     { get; private set; }
    public string           OriginalReferenceNo { get; private set; } = default!;
    public Money            TotalRefundAmount  { get; private set; } = Money.Zero;
    public ReturnResolution Resolution         { get; private set; }
    public string           Reason             { get; private set; } = default!;
    public string           ReturnedBy         { get; private set; } = default!;
    public DateTime         ReturnedAt         { get; private set; }

    public IReadOnlyList<SaleReturnLine> Lines => _lines.AsReadOnly();

    private SaleReturn() { }

    public static SaleReturn Record(
        SaleId           originalSaleId,
        string           originalReferenceNo,
        IReadOnlyList<SaleReturnLineInput> lines,
        ReturnResolution resolution,
        string           reason,
        string           returnedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Alasan retur tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(returnedBy))
            throw new DomainException("Staff yang mencatat retur tidak boleh kosong.");
        if (lines.Count == 0)
            throw new DomainException("Retur harus memiliki minimal satu item.");
        if (lines.Any(l => l.Quantity <= 0))
            throw new DomainException("Kuantitas retur harus lebih dari nol.");

        var lineData = lines.Select(l => new SaleReturnLineData(
            l.ProductId, l.ProductName, l.VariantId, l.ColorName,
            l.UnitName, l.Quantity, l.QuantityInBaseUnit, l.UnitPrice)).ToList();

        var totalRefund = lineData.Sum(l => l.Quantity * l.UnitPrice);

        var ret = new SaleReturn();
        ret.Raise(new SaleReturnRecorded(
            SaleReturnId.New(),
            originalSaleId,
            originalReferenceNo.Trim(),
            lineData.AsReadOnly(),
            totalRefund,
            resolution,
            reason.Trim(),
            returnedBy.Trim(),
            DateTime.UtcNow));
        return ret;
    }

    public static SaleReturn Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var ret = new SaleReturn();
        ret.Load(events);
        return ret;
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        if (domainEvent is not SaleReturnRecorded e) return;

        Id                 = e.ReturnId;
        OriginalSaleId     = e.OriginalSaleId;
        OriginalReferenceNo = e.OriginalReferenceNo;
        TotalRefundAmount  = new Money(e.TotalRefundAmount);
        Resolution         = e.Resolution;
        Reason             = e.Reason;
        ReturnedBy         = e.ReturnedBy;
        ReturnedAt         = e.OccurredAt;
        _lines.AddRange(e.Lines.Select(l =>
            new SaleReturnLine(l.ProductId, l.ProductName, l.VariantId, l.ColorName,
                               l.UnitName, l.Quantity, l.QuantityInBaseUnit, l.UnitPrice)));
    }
}
```

**File baru:** `Materia.Backend/Materia.Domain/Sales/SaleReturnLine.cs`

```csharp
namespace Materia.Domain.Sales;

public sealed class SaleReturnLine
{
    public Guid    ProductId          { get; }
    public string  ProductName        { get; }
    public Guid?   VariantId          { get; }
    public string? ColorName          { get; }
    public string  UnitName           { get; }
    public decimal Quantity           { get; }
    public decimal QuantityInBaseUnit { get; }
    public decimal UnitPrice          { get; }
    public decimal Subtotal           => Quantity * UnitPrice;

    internal SaleReturnLine(
        Guid productId, string productName, Guid? variantId, string? colorName,
        string unitName, decimal quantity, decimal quantityInBaseUnit, decimal unitPrice)
    {
        ProductId = productId; ProductName = productName;
        VariantId = variantId; ColorName = colorName;
        UnitName = unitName; Quantity = quantity;
        QuantityInBaseUnit = quantityInBaseUnit; UnitPrice = unitPrice;
    }
}
```

**File baru:** `Materia.Backend/Materia.Domain/Sales/SaleReturnLineInput.cs`

```csharp
namespace Materia.Domain.Sales;

public sealed record SaleReturnLineInput(
    Guid    ProductId,
    string  ProductName,
    Guid?   VariantId,
    string? ColorName,
    string  UnitName,
    decimal Quantity,
    decimal QuantityInBaseUnit,
    decimal UnitPrice);
```

### 1.5 Customer — Pengurangan Hutang untuk Retur

**File baru:** `Materia.Backend/Materia.Domain/Customers/Events/CustomerDebtReducedForReturn.cs`

```csharp
using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerDebtReducedForReturn(
    CustomerId CustomerId,
    Guid       SaleReturnId,
    decimal    ReducedAmount,
    decimal    OutstandingDebtAfter,
    string     ReducedBy,
    DateTime   OccurredAt) : IDomainEvent;
```

**Tambah ke `Customer.cs`** (setelah `RecordRepayment`):

```csharp
/// <summary>
/// Reduces the customer's outstanding debt as a result of a sales return.
/// No FIFO allocation — this is a direct debt adjustment, not a cash payment.
/// </summary>
public void ReduceDebtForReturn(decimal amount, Guid saleReturnId, string reducedBy)
{
    if (amount <= 0)
        throw new DomainException("Jumlah pengurangan piutang harus lebih dari nol.");
    if (amount > OutstandingDebt)
        throw new DomainException("Jumlah pengurangan melebihi sisa piutang pelanggan.");

    Raise(new CustomerDebtReducedForReturn(
        Id, saleReturnId, amount,
        OutstandingDebt - amount,
        reducedBy, DateTime.UtcNow));
}
```

**Tambah case di `Customer.Apply()`**:

```csharp
case CustomerDebtReducedForReturn e:
    OutstandingDebt = e.OutstandingDebtAfter;
    // Reduce oldest open receivable lines proportionally (FIFO)
    var remaining = e.ReducedAmount;
    foreach (var line in _openReceivables.OrderBy(l => l.IncurredAt))
    {
        if (remaining <= 0) break;
        var applied = Math.Min(remaining, line.RemainingAmount);
        line.ApplyPayment(applied);
        remaining -= applied;
    }
    break;
```

> **Catatan:** `ReceivableLine.ApplyPayment()` sudah ada untuk `ReceivablePaymentRecorded`. Reuse method yang sama.

---

## Fase 2 — Tests (TDD — tulis sebelum implementasi handler)

**File baru:** `Materia.Tests/Sales/SaleReturnAggregateTests.cs`

Skenario yang harus dicover:
- ✅ `Record()` sukses → event `SaleReturnRecorded` diemit
- ✅ Reconstitute dari events → state terbentuk benar
- ✅ Reason kosong → `DomainException`
- ✅ Lines kosong → `DomainException`
- ✅ Quantity ≤ 0 → `DomainException`
- ✅ `TotalRefundAmount` = sum(qty × unitPrice) per line

**File baru:** `Materia.Tests/Sales/RecordSaleReturnValidatorTests.cs`

Skenario:
- ✅ SaleId tidak ada di sistem → gagal
- ✅ Sale belum lunas (Draft/Confirmed) → gagal
- ✅ Qty retur > qty original → gagal
- ✅ DebtReduction tapi bukan pelanggan terdaftar → gagal
- ✅ DebtReduction tapi amount > OutstandingDebt → gagal

---

## Fase 3 — Application

### 3.1 Contract

**File baru:** `Materia.Backend/Materia.Application/Contracts/Sales/ISaleReturnRepository.cs`

```csharp
using Materia.Domain.Sales;

namespace Materia.Application.Contracts.Sales;

public interface ISaleReturnRepository
{
    Task SaveAsync(SaleReturn saleReturn, CancellationToken ct = default);
}
```

### 3.2 Command

**File baru:** `Materia.Backend/Materia.Application/Commands/Sales/RecordSaleReturn/RecordSaleReturnCommand.cs`

```csharp
namespace Materia.Application.Commands.Sales.RecordSaleReturn;

public sealed record RecordSaleReturnCommand(
    Guid                        OriginalSaleId,
    IReadOnlyList<ReturnLineDto> Lines,
    string                      Resolution,  // "CashRefund" | "DebtReduction"
    string                      Reason,
    string                      ReturnedBy);

public sealed record ReturnLineDto(
    Guid    ProductId,
    Guid?   VariantId,
    string  UnitName,
    decimal Quantity);
```

### 3.3 Validator

**File baru:** `Materia.Backend/Materia.Application/Commands/Sales/RecordSaleReturn/RecordSaleReturnCommandValidator.cs`

Validasi dengan FluentValidation:
- `OriginalSaleId` tidak boleh `Guid.Empty`
- `Lines` tidak boleh kosong
- Tiap line: `Quantity > 0`, `ProductId != Guid.Empty`
- `Resolution` harus salah satu dari `CashRefund` / `DebtReduction`
- `Reason` tidak boleh kosong

### 3.4 Handler

**File baru:** `Materia.Backend/Materia.Application/Commands/Sales/RecordSaleReturn/RecordSaleReturnCommandHandler.cs`

Dependensi:
- `ISaleRepository` — load original sale untuk validasi + ambil item metadata
- `ISaleReturnRepository` — save SaleReturn aggregate
- `ICustomerRepository` — load customer jika DebtReduction
- `IStockDeductionService` → **gunakan qty positif** (atau inject `AdjustStockCommandHandler` langsung dengan qty +)
- `ISaleQueryRepository` → baca read model sale untuk validasi qty

Langkah handler:
1. Load Sale read model → validasi status (`Paid` atau `PartiallyPaid`)
2. Untuk tiap line: validasi `qty retur ≤ qty original` dari `SaleItemReadModel`
3. Resolve product metadata (nama, unitName, QuantityInBaseUnit) dari Sale items
4. Parse `ReturnResolution` enum dari string
5. Buat `SaleReturn.Record(...)`
6. Save SaleReturn aggregate
7. Restore stok: `AdjustStockCommand(productId, +quantityInBaseUnit, "Retur penjualan {refNo}", returnedBy, variantId)`
8. Jika `DebtReduction`: load Customer → `customer.ReduceDebtForReturn(totalRefundAmount, returnId, returnedBy)` → save Customer

---

## Fase 4 — Infrastructure

### 4.1 Read Model

**File baru:** `Materia.Backend/Materia.Infrastructure/Persistence/Projections/SaleReturnReadModel.cs`

```csharp
using Materia.Domain.Sales;

namespace Materia.Infrastructure.Persistence.Projections;

public class SaleReturnReadModel
{
    public Guid             Id                  { get; set; }
    public Guid             StoreId             { get; set; }
    public Guid             OriginalSaleId      { get; set; }
    public string           OriginalReferenceNo { get; set; } = default!;
    public decimal          TotalRefundAmount   { get; set; }
    public ReturnResolution Resolution          { get; set; }
    public string           Reason              { get; set; } = default!;
    public string           ReturnedBy          { get; set; } = default!;
    public DateTime         ReturnedAt          { get; set; }

    public List<SaleReturnLineReadModel> Lines { get; set; } = [];
}
```

**File baru:** `Materia.Backend/Materia.Infrastructure/Persistence/Projections/SaleReturnLineReadModel.cs`

```csharp
namespace Materia.Infrastructure.Persistence.Projections;

public class SaleReturnLineReadModel
{
    public Guid    Id                 { get; set; }
    public Guid    SaleReturnId       { get; set; }
    public Guid    StoreId            { get; set; }
    public Guid    ProductId          { get; set; }
    public Guid?   VariantId          { get; set; }
    public string  ProductName        { get; set; } = default!;
    public string? ColorName          { get; set; }
    public string  UnitName           { get; set; } = default!;
    public decimal Quantity           { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public decimal UnitPrice          { get; set; }
    public decimal Subtotal           { get; set; }
}
```

### 4.2 Projection (Event → Read Model)

**File baru:** `Materia.Backend/Materia.Infrastructure/Persistence/Projections/SaleReturnProjection.cs`

Mapping `SaleReturnRecorded` → upsert `SaleReturnReadModel` + `SaleReturnLineReadModel`. Ikuti pola `SaleProjection` yang sudah ada.

### 4.3 Repository

**File baru:** `Materia.Backend/Materia.Infrastructure/Sales/SaleReturnRepository.cs`

Implementasi `ISaleReturnRepository`. Ikuti pola `SaleRepository` — serialize event ke `StoredEvents`, lalu trigger projection.

### 4.4 Query Repository (opsional di fase ini)

**File baru:** `Materia.Backend/Materia.Infrastructure/Sales/SaleReturnQueryRepository.cs`

Method awal:
- `GetBySaleIdAsync(Guid saleId)` → list retur untuk satu sale (dipakai di SaleDetail UI)

### 4.5 EF Core

- Tambah `DbSet<SaleReturnReadModel>` dan `DbSet<SaleReturnLineReadModel>` ke `AppDbContext`
- Tambah konfigurasi EF di folder `Configurations/`
- Jalankan migrasi:
  ```
  dotnet ef migrations add AddSaleReturnReadModel --project Materia.Infrastructure --startup-project Materia.WebApi
  ```

### 4.6 DI Registration

Di `Program.cs` / `ServiceCollectionExtensions`:
- `services.AddScoped<ISaleReturnRepository, SaleReturnRepository>()`
- `services.AddScoped<RecordSaleReturnCommandHandler>()`
- `services.AddScoped<RecordSaleReturnCommandValidator>()`

---

## Fase 5 — API

**File:** `Materia.Backend/Materia.WebApi/Controllers/Sales/SalesController.cs`

Tambah endpoint baru:

```
POST /api/sales/{id}/returns
```

Request body:
```json
{
  "lines": [
    { "productId": "...", "variantId": null, "unitName": "pcs", "quantity": 2 }
  ],
  "resolution": "CashRefund",
  "reason": "Barang tidak sesuai ukuran",
  "returnedBy": "Budi"
}
```

Response `201 Created`:
```json
{
  "returnId": "...",
  "originalReferenceNo": "INV-20260621-001",
  "totalRefundAmount": 50000,
  "resolution": "CashRefund"
}
```

Authorization: `Admin`, `Cashier` (bukan `Gudang`).

---

## Fase 6 — WebUi (Blazor)

### 6.1 Tombol Retur di SaleDetail

**File:** `Materia.WebUi/Materia.WebUi/Components/Pages/Sales/SaleDetail.razor`

- Tampilkan tombol "Retur Barang" hanya jika `Sale.Status == Paid || PartiallyPaid`
- Klik → buka `ReturnDialog` (MudDialog)

### 6.2 Dialog Retur

**File baru:** `Materia.WebUi/Materia.WebUi/Components/Pages/Sales/ReturnDialog.razor`

Komponen:
- List item sale dengan input qty retur (validasi: 0 ≤ qty ≤ qty original)
- Dropdown resolusi: "Refund Tunai" / "Kurangi Piutang" (opsi kedua hanya muncul jika `OutstandingAmount > 0`)
- Input alasan (wajib)
- Tombol "Simpan Retur" → POST `/api/sales/{id}/returns`

### 6.3 Tampilkan Retur di SaleDetail

Di bawah tabel items, tambahkan section "Riwayat Retur" yang menampilkan list retur dari `GET /api/sales/{id}/returns` (endpoint baca, tambahkan di API juga).

---

## Checklist Eksekusi

```
[ ] Fase 1 — Domain
    [ ] SaleReturnId, SaleReturnEnums
    [ ] SaleReturnRecorded event
    [ ] SaleReturnLine, SaleReturnLineInput
    [ ] SaleReturn aggregate
    [ ] CustomerDebtReducedForReturn event
    [ ] Customer.ReduceDebtForReturn() + Apply case

[ ] Fase 2 — Tests (tulis SEBELUM handler!)
    [ ] SaleReturnAggregateTests
    [ ] RecordSaleReturnValidatorTests

[ ] Fase 3 — Application
    [ ] ISaleReturnRepository contract
    [ ] RecordSaleReturnCommand + DTO
    [ ] RecordSaleReturnCommandValidator
    [ ] RecordSaleReturnCommandHandler

[ ] Fase 4 — Infrastructure
    [ ] SaleReturnReadModel + SaleReturnLineReadModel
    [ ] SaleReturnProjection
    [ ] SaleReturnRepository
    [ ] SaleReturnQueryRepository
    [ ] AppDbContext DbSet + konfigurasi EF
    [ ] EF migration
    [ ] DI registration

[ ] Fase 5 — API
    [ ] POST /api/sales/{id}/returns
    [ ] GET /api/sales/{id}/returns (untuk UI)

[ ] Fase 6 — WebUi
    [ ] Tombol Retur di SaleDetail
    [ ] ReturnDialog component
    [ ] Section riwayat retur di SaleDetail
```

---

## Aturan Bisnis Penting

1. Retur hanya bisa dilakukan pada sale berstatus **`Paid` atau `PartiallyPaid`** (sudah difinalisasi).
2. Qty retur per item **tidak boleh melebihi** qty original di sale tersebut.
3. `DebtReduction` hanya tersedia jika sale punya `OutstandingAmount > 0` (penjualan kredit).
4. Stok yang diretur selalu dikembalikan ke sistem, **tanpa peduli kondisi barang** (kondisi barang adalah tanggung jawab gudang secara fisik).
5. Satu sale bisa memiliki **beberapa transaksi retur** (retur parsial diperbolehkan).
6. Tidak ada undo/pembatalan retur — jika salah, buat penjualan baru.

# Sprint 4 — Stock Opname (Physical Inventory Count)

> **Status:** Siap dieksekusi setelah Sprint 3 selesai.

---

## Context

Secara berkala (mingguan/bulanan) toko perlu menghitung fisik semua stok dan merekonsiliasi dengan angka sistem. Saat ini tidak ada sesi opname terstruktur — staff hanya bisa adjustment manual per produk satu-per-satu, yang tidak meninggalkan audit trail sebagai satu sesi opname.

Stock Opname membutuhkan aggregate baru dengan lifecycle yang jelas: **Draft → InProgress → Completed** (atau Cancelled).

---

## Keputusan Desain

| Keputusan | Pilihan | Alasan |
|---|---|---|
| Scope opname | Per-toko, semua produk aktif | Opname parsial (per kategori) bisa ditambah nanti |
| Trigger adjustment stok | Pada `Complete()`: emit satu `StockOpnameCompleted` → handler batch-adjust tiap produk yang ada selisih | Atomic; tidak ada state intermediate yang setengah-selesai |
| Metode input aktual | User input qty aktual per baris (bukan selisih) | Lebih alami untuk counter yang menghitung fisik |
| Produk tanpa selisih | Tidak dibuatkan `StockAdjusted` event | Mengurangi noise di event store |
| Lock selama opname | Tidak ada hard lock di domain — operasional tetap jalan, tapi UI beri peringatan "ada opname aktif" | Menyederhanakan implementasi; opname completed mengambil snapshot qty saat itu |

---

## Fase 1 — Domain

### 1.1 Typed ID

**File baru:** `Materia.Backend/Materia.Domain/Inventory/StockOpnameId.cs`

```csharp
namespace Materia.Domain.Inventory;

public readonly record struct StockOpnameId(Guid Value)
{
    public static StockOpnameId New() => new(Guid.NewGuid());
    public static StockOpnameId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
```

### 1.2 Enum

**File baru:** `Materia.Backend/Materia.Domain/Inventory/StockOpnameEnums.cs`

```csharp
namespace Materia.Domain.Inventory;

// NOTE: serialize by ordinal — only APPEND, never reorder.
public enum StockOpnameStatus { Draft, InProgress, Completed, Cancelled }
```

### 1.3 Domain Events

**File baru:** `Events/StockOpnameCreated.cs`

```csharp
public record StockOpnameCreated(
    StockOpnameId OpnameId,
    string        CreatedBy,
    DateTime      OccurredAt) : IDomainEvent;
```

**File baru:** `Events/StockOpnameStarted.cs`

```csharp
public record StockOpnameStarted(
    StockOpnameId OpnameId,
    IReadOnlyList<OpnameLineSnapshot> Lines,  // snapshot qty sistem saat opname dimulai
    string        StartedBy,
    DateTime      OccurredAt) : IDomainEvent;

public record OpnameLineSnapshot(
    Guid    ProductId,
    string  ProductName,
    string  BaseUnit,
    Guid?   VariantId,
    string? ColorName,
    decimal SystemQty);
```

**File baru:** `Events/StockOpnameLineUpdated.cs`

```csharp
public record StockOpnameLineUpdated(
    StockOpnameId OpnameId,
    Guid          ProductId,
    Guid?         VariantId,
    decimal       ActualQty,
    string        UpdatedBy,
    DateTime      OccurredAt) : IDomainEvent;
```

**File baru:** `Events/StockOpnameCompleted.cs`

```csharp
public record StockOpnameCompleted(
    StockOpnameId OpnameId,
    IReadOnlyList<OpnameAdjustment> Adjustments,  // hanya yang ada selisih
    string        CompletedBy,
    DateTime      OccurredAt) : IDomainEvent;

public record OpnameAdjustment(
    Guid    ProductId,
    Guid?   VariantId,
    decimal SystemQty,
    decimal ActualQty,
    decimal Delta);   // ActualQty - SystemQty
```

**File baru:** `Events/StockOpnameCancelled.cs`

```csharp
public record StockOpnameCancelled(
    StockOpnameId OpnameId,
    string        Reason,
    string        CancelledBy,
    DateTime      OccurredAt) : IDomainEvent;
```

### 1.4 Aggregate

**File baru:** `Materia.Backend/Materia.Domain/Inventory/StockOpname.cs`

```csharp
using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;

namespace Materia.Domain.Inventory;

public sealed class StockOpname : AggregateRoot<StockOpnameId>
{
    private readonly List<OpnameLine> _lines = [];

    public StockOpnameStatus Status    { get; private set; }
    public string            CreatedBy { get; private set; } = default!;
    public DateTime          CreatedAt { get; private set; }

    public IReadOnlyList<OpnameLine> Lines => _lines.AsReadOnly();

    private StockOpname() { }

    public static StockOpname Create(string createdBy)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new DomainException("Staff pembuat opname tidak boleh kosong.");

        var opname = new StockOpname();
        opname.Raise(new StockOpnameCreated(StockOpnameId.New(), createdBy, DateTime.UtcNow));
        return opname;
    }

    public static StockOpname Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var opname = new StockOpname();
        opname.Load(events);
        return opname;
    }

    /// <summary>Mulai opname: ambil snapshot qty sistem saat ini, status jadi InProgress.</summary>
    public void Start(IReadOnlyList<OpnameLineSnapshot> systemSnapshot, string startedBy)
    {
        if (Status != StockOpnameStatus.Draft)
            throw new DomainException("Opname hanya bisa dimulai dari status Draft.");
        if (systemSnapshot.Count == 0)
            throw new DomainException("Tidak ada produk aktif untuk diopname.");

        Raise(new StockOpnameStarted(Id, systemSnapshot, startedBy, DateTime.UtcNow));
    }

    /// <summary>Update qty aktual yang dihitung fisik untuk satu produk.</summary>
    public void UpdateLine(Guid productId, Guid? variantId, decimal actualQty, string updatedBy)
    {
        if (Status != StockOpnameStatus.InProgress)
            throw new DomainException("Hanya bisa mengisi qty saat opname InProgress.");
        if (actualQty < 0)
            throw new DomainException("Qty aktual tidak boleh negatif.");
        if (_lines.All(l => l.ProductId != productId || l.VariantId != variantId))
            throw new DomainException("Produk tidak ditemukan dalam sesi opname ini.");

        Raise(new StockOpnameLineUpdated(Id, productId, variantId, actualQty, updatedBy, DateTime.UtcNow));
    }

    /// <summary>Selesaikan opname: hitung selisih, siapkan data untuk batch-adjust stok.</summary>
    public IReadOnlyList<OpnameAdjustment> Complete(string completedBy)
    {
        if (Status != StockOpnameStatus.InProgress)
            throw new DomainException("Hanya opname InProgress yang bisa diselesaikan.");

        var adjustments = _lines
            .Where(l => l.ActualQty.HasValue && l.ActualQty.Value != l.SystemQty)
            .Select(l => new OpnameAdjustment(
                l.ProductId, l.VariantId,
                l.SystemQty, l.ActualQty!.Value,
                l.ActualQty.Value - l.SystemQty))
            .ToList()
            .AsReadOnly();

        Raise(new StockOpnameCompleted(Id, adjustments, completedBy, DateTime.UtcNow));
        return adjustments;
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status == StockOpnameStatus.Completed || Status == StockOpnameStatus.Cancelled)
            throw new DomainException($"Opname dengan status {Status} tidak bisa dibatalkan.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Alasan pembatalan tidak boleh kosong.");

        Raise(new StockOpnameCancelled(Id, reason, cancelledBy, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case StockOpnameCreated e:
                Id        = e.OpnameId;
                Status    = StockOpnameStatus.Draft;
                CreatedBy = e.CreatedBy;
                CreatedAt = e.OccurredAt;
                break;

            case StockOpnameStarted e:
                Status = StockOpnameStatus.InProgress;
                _lines.AddRange(e.Lines.Select(l => new OpnameLine(
                    l.ProductId, l.ProductName, l.BaseUnit, l.VariantId, l.ColorName, l.SystemQty)));
                break;

            case StockOpnameLineUpdated e:
                var line = _lines.First(l => l.ProductId == e.ProductId && l.VariantId == e.VariantId);
                line.SetActualQty(e.ActualQty);
                break;

            case StockOpnameCompleted:
                Status = StockOpnameStatus.Completed;
                break;

            case StockOpnameCancelled:
                Status = StockOpnameStatus.Cancelled;
                break;
        }
    }
}
```

**File baru:** `Materia.Backend/Materia.Domain/Inventory/OpnameLine.cs`

```csharp
namespace Materia.Domain.Inventory;

public sealed class OpnameLine
{
    public Guid    ProductId   { get; }
    public string  ProductName { get; }
    public string  BaseUnit    { get; }
    public Guid?   VariantId   { get; }
    public string? ColorName   { get; }
    public decimal SystemQty   { get; }
    public decimal? ActualQty  { get; private set; }   // null = belum diisi
    public decimal? Delta      => ActualQty.HasValue ? ActualQty.Value - SystemQty : null;

    internal OpnameLine(
        Guid productId, string productName, string baseUnit,
        Guid? variantId, string? colorName, decimal systemQty)
    {
        ProductId = productId; ProductName = productName; BaseUnit = baseUnit;
        VariantId = variantId; ColorName = colorName; SystemQty = systemQty;
    }

    internal void SetActualQty(decimal qty) => ActualQty = qty;
}
```

---

## Fase 2 — Tests

**File baru:** `Materia.Tests/Inventory/StockOpnameAggregateTests.cs`

- ✅ Create → status Draft, event emitted
- ✅ Start dengan snapshot valid → InProgress, lines terbentuk
- ✅ Start dari non-Draft → DomainException
- ✅ UpdateLine sukses → ActualQty diset
- ✅ UpdateLine produk tidak ada → DomainException
- ✅ UpdateLine qty negatif → DomainException
- ✅ Complete → adjustments berisi hanya produk dengan selisih, status Completed
- ✅ Complete dari non-InProgress → DomainException
- ✅ Complete dengan semua qty sama → adjustments kosong, tetap completed
- ✅ Cancel → status Cancelled
- ✅ Cancel dari Completed → DomainException

---

## Fase 3 — Application

### Contract

**File baru:** `Contracts/Inventory/IStockOpnameRepository.cs`

```csharp
public interface IStockOpnameRepository
{
    Task<StockOpname?> GetActiveAsync(CancellationToken ct = default);  // max 1 aktif per toko
    Task SaveAsync(StockOpname opname, CancellationToken ct = default);
}
```

### Commands

```
CreateStockOpname/
  CreateStockOpnameCommand.cs       — { CreatedBy }
  CreateStockOpnameCommandHandler.cs — cek tidak ada opname aktif, Create(), save
  
StartStockOpname/
  StartStockOpnameCommand.cs        — { OpnameId, StartedBy }
  StartStockOpnameCommandHandler.cs — load semua StockReadModel sebagai snapshot, Start(), save

UpdateStockOpnameLine/
  UpdateStockOpnameLineCommand.cs   — { OpnameId, ProductId, VariantId?, ActualQty, UpdatedBy }
  UpdateStockOpnameLineCommandHandler.cs

CompleteStockOpname/
  CompleteStockOpnameCommand.cs     — { OpnameId, CompletedBy }
  CompleteStockOpnameCommandHandler.cs
    → Complete() → dapat adjustments
    → Untuk tiap adjustment dengan delta ≠ 0:
         AdjustStockCommand(productId, delta, "Stock opname {date}", completedBy, variantId)
    → save StockOpname

CancelStockOpname/
  CancelStockOpnameCommand.cs
  CancelStockOpnameCommandHandler.cs
```

**Aturan:** Hanya boleh ada **satu opname aktif** (Draft atau InProgress) per toko dalam satu waktu.

---

## Fase 4 — Infrastructure

### Read Model

**File baru:** `StockOpnameReadModel.cs`

```csharp
public class StockOpnameReadModel
{
    public Guid              Id          { get; set; }
    public Guid              StoreId     { get; set; }
    public string            Status      { get; set; } = default!;
    public string            CreatedBy   { get; set; } = default!;
    public DateTime          CreatedAt   { get; set; }
    public DateTime?         CompletedAt { get; set; }
    public int               TotalLines  { get; set; }
    public int               FilledLines { get; set; }  // yang sudah diisi ActualQty
    public int               AdjustedLines { get; set; }  // yang ada selisih
    public string            LinesJson   { get; set; } = "[]";  // snapshot + actual + delta
}
```

### EF Core

```
dotnet ef migrations add AddStockOpname --project Materia.Infrastructure --startup-project Materia.WebApi
```

---

## Fase 5 — API

```
POST /api/stock-opname
→ Buat sesi opname baru (cek tidak ada yang aktif)

POST /api/stock-opname/{id}/start
→ Mulai opname (ambil snapshot stok sistem)

PUT /api/stock-opname/{id}/lines
Body: { "productId": "...", "variantId": null, "actualQty": 45 }
→ Isi qty aktual satu baris

POST /api/stock-opname/{id}/complete
→ Selesaikan dan trigger batch stock adjustment

POST /api/stock-opname/{id}/cancel
Body: { "reason": "..." }

GET /api/stock-opname
→ List riwayat opname (paginated)

GET /api/stock-opname/active
→ Opname yang sedang berjalan (jika ada)

GET /api/stock-opname/{id}
→ Detail satu sesi opname (termasuk semua lines)
```

Authorization: `Admin`, `Gudang` (untuk input); hanya `Admin` untuk Create/Complete/Cancel.

---

## Fase 6 — WebUi

### 6.1 Halaman List Opname

**File baru:** `Pages/Inventory/StockOpname.razor`

- Tabel riwayat sesi opname (tanggal, status, total produk, produk ada selisih, oleh siapa)
- Tombol "Mulai Opname Baru" (disabled jika ada yang aktif)
- Link ke detail sesi

### 6.2 Halaman Detail / Input Opname

**File baru:** `Pages/Inventory/StockOpnameDetail.razor`

- Header: status, tanggal mulai, progress (X dari Y produk sudah diisi)
- Tabel produk dengan kolom: Nama | Satuan | Qty Sistem | Qty Aktual (input) | Selisih
  - Baris yang belum diisi: input kosong (highlight kuning)
  - Selisih positif (surplus) → hijau, negatif (kurang) → merah
- Tombol "Selesaikan Opname" → konfirmasi dialog dengan ringkasan selisih
- Tombol "Batalkan Opname"

### 6.3 Laporan Hasil Opname

Setelah completed, tampilkan ringkasan:
- Total produk diperiksa
- Produk dengan selisih: N produk
- Nilai selisih (positif/negatif) berdasarkan average cost

---

## Checklist Eksekusi

```
[ ] Fase 1 — Domain
    [ ] StockOpnameId, StockOpnameEnums
    [ ] 5 domain events (Created, Started, LineUpdated, Completed, Cancelled)
    [ ] OpnameLine entity
    [ ] StockOpname aggregate (Create, Start, UpdateLine, Complete, Cancel)

[ ] Fase 2 — Tests
    [ ] StockOpnameAggregateTests (semua skenario)

[ ] Fase 3 — Application
    [ ] IStockOpnameRepository contract
    [ ] 5 commands + validators + handlers

[ ] Fase 4 — Infrastructure
    [ ] StockOpnameReadModel
    [ ] StockOpnameProjection
    [ ] StockOpnameRepository
    [ ] StockOpnameQueryRepository
    [ ] EF migration

[ ] Fase 5 — API
    [ ] 7 endpoint (POST create, POST start, PUT line, POST complete, POST cancel, GET list, GET active, GET detail)

[ ] Fase 6 — WebUi
    [ ] StockOpname.razor (list)
    [ ] StockOpnameDetail.razor (input + complete)
    [ ] Nav menu entry
```

---

## Aturan Bisnis Penting

1. Hanya boleh **satu sesi opname aktif** (Draft atau InProgress) per toko.
2. Snapshot qty sistem diambil pada saat `Start()` — bukan saat `Create()` — agar mencerminkan kondisi terkini.
3. Produk yang **tidak aktif** tidak dimasukkan dalam snapshot.
4. `Complete()` hanya menghasilkan `StockAdjusted` untuk produk yang **ada selisih** (delta ≠ 0).
5. Produk yang tidak diisi `ActualQty` (belum dihitung) **tidak diadjust** — dianggap masih perlu dihitung. UI harus memperingatkan sebelum complete jika masih ada yang kosong.
6. Setelah completed, opname **tidak bisa diubah** — jika ada koreksi, buat opname baru.

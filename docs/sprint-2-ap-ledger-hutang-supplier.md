# Sprint 2 — AP Ledger / Hutang ke Supplier

> **Status:** Siap dieksekusi setelah Sprint 1 (Retur Penjualan) selesai.

---

## Context

PurchaseOrder sudah menyimpan `PaymentTerm` (tenor/jatuh tempo) sejak fitur termin ditambahkan, tapi tidak ada cara mencatat bahwa hutang itu sudah dibayar. Toko tidak tahu berapa total hutang ke supplier saat ini. Ini sisi yang hilang dari siklus keuangan: piutang dari pelanggan (`CustomerDebtIncurred`) sudah ada, tapi hutang ke supplier belum.

---

## Keputusan Desain

| Keputusan | Pilihan | Alasan |
|---|---|---|
| Tempat event pembayaran | Command `PurchaseOrder.RecordPayment()` → `PurchaseOrderPaymentRecorded` | Hutang lahir dari PO, pembayaran menyesuaikan saldo PO — lebih kohesif daripada aggregate terpisah |
| Tracking saldo hutang | Tambah `OutstandingAmount` + `PaidAmount` ke `PurchaseOrderReadModel` | Cukup untuk query list + total hutang per supplier |
| Pembayaran parsial | Diperbolehkan (`PaidAmount < TotalAmount`) | Lazim di toko: bayar sebagian sesuai cash flow |
| Jatuh tempo (due date) | Sudah ada `PaymentDueDate` di read model (dari tenor + ReceivedAt). Tinggal expose ke UI | — |

---

## Fase 1 — Domain

### 1.1 Event Baru

**File baru:** `Materia.Backend/Materia.Domain/Purchasing/Events/PurchaseOrderPaymentRecorded.cs`

```csharp
using Materia.Domain.Common;
using Materia.Domain.Sales;

namespace Materia.Domain.Purchasing.Events;

public record PurchaseOrderPaymentRecorded(
    PurchaseOrderId PurchaseOrderId,
    Guid            PaymentId,
    decimal         AmountPaid,
    decimal         TotalPaidAfter,
    decimal         OutstandingAfter,
    PaymentMethod   Method,
    string?         Notes,
    string          PaidBy,
    DateTime        OccurredAt) : IDomainEvent;
```

### 1.2 State baru di PurchaseOrder

Tambah property ke `PurchaseOrder.cs`:

```csharp
public decimal TotalPaid       { get; private set; }
public decimal OutstandingDebt => TotalCost - TotalPaid;  // TotalCost = sum(line.NetReceivedQty * line.UnitCost)
```

> `TotalCost` dihitung dari Lines yang sudah diterima (NetReceivedQty × UnitCost). Ini hanya relevan setelah PO minimal `PartiallyReceived`.

### 1.3 Command baru di PurchaseOrder

Tambah method ke `PurchaseOrder.cs`:

```csharp
public Guid RecordPayment(
    decimal amount, PaymentMethod method, string? notes, string paidBy)
{
    if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Confirmed or PurchaseOrderStatus.Cancelled)
        throw new DomainException("Pembayaran hanya bisa dicatat setelah barang diterima.");
    if (amount <= 0)
        throw new DomainException("Jumlah pembayaran harus lebih dari nol.");

    var totalCost = Lines.Sum(l => l.NetReceivedQty * l.UnitCost);
    if (TotalPaid + amount > totalCost)
        throw new DomainException("Jumlah pembayaran melebihi total hutang PO ini.");

    var paymentId = Guid.NewGuid();
    Raise(new PurchaseOrderPaymentRecorded(
        Id, paymentId, amount,
        TotalPaid + amount,
        totalCost - (TotalPaid + amount),
        method, notes?.Trim(), paidBy, DateTime.UtcNow));
    return paymentId;
}
```

Apply di `PurchaseOrder.Apply()`:

```csharp
case PurchaseOrderPaymentRecorded e:
    TotalPaid = e.TotalPaidAfter;
    break;
```

---

## Fase 2 — Tests

**File baru:** `Materia.Tests/Purchasing/PurchaseOrderPaymentTests.cs`

Skenario:
- ✅ `RecordPayment()` pada PO `Received` → event emitted, `TotalPaid` diupdate
- ✅ `RecordPayment()` pada PO `PartiallyReceived` → diperbolehkan
- ✅ `RecordPayment()` pada PO `Draft` / `Confirmed` / `Cancelled` → `DomainException`
- ✅ Pembayaran melebihi total hutang → `DomainException`
- ✅ Pembayaran parsial → `OutstandingDebt` berkurang sesuai
- ✅ Pembayaran penuh → `OutstandingDebt == 0`

---

## Fase 3 — Application

### 3.1 Command

**File baru:** `Materia.Backend/Materia.Application/Commands/Purchasing/RecordPurchaseOrderPayment/RecordPurchaseOrderPaymentCommand.cs`

```csharp
namespace Materia.Application.Commands.Purchasing.RecordPurchaseOrderPayment;

public sealed record RecordPurchaseOrderPaymentCommand(
    Guid   PurchaseOrderId,
    decimal Amount,
    string  Method,    // e.g. "Cash", "BankTransfer", "QRIS"
    string? Notes,
    string  PaidBy);
```

### 3.2 Validator

FluentValidation:
- `PurchaseOrderId != Guid.Empty`
- `Amount > 0`
- `Method` valid enum value
- `PaidBy` tidak kosong

### 3.3 Handler

**File baru:** `RecordPurchaseOrderPaymentCommandHandler.cs`

```csharp
public async Task HandleAsync(RecordPurchaseOrderPaymentCommand command, CancellationToken ct = default)
{
    var po = await poRepository.GetByIdAsync(PurchaseOrderId.From(command.PurchaseOrderId), ct)
        ?? throw new DomainException($"Purchase order {command.PurchaseOrderId} tidak ditemukan.");

    var method = Enum.Parse<PaymentMethod>(command.Method, ignoreCase: true);
    po.RecordPayment(command.Amount, method, command.Notes, command.PaidBy);

    await poRepository.SaveAsync(po, ct);
}
```

---

## Fase 4 — Infrastructure

### 4.1 Update PurchaseOrderReadModel

Tambah kolom ke `PurchaseOrderReadModel.cs`:

```csharp
public decimal TotalCost        { get; set; }   // sum(NetReceivedQty × UnitCost) — diupdate tiap receipt
public decimal TotalPaid        { get; set; }
public decimal OutstandingAmount => TotalCost - TotalPaid;
public bool    IsFullyPaid      => OutstandingAmount <= 0;
```

### 4.2 PurchaseOrderPaymentReadModel (opsional, untuk riwayat)

**File baru:** `PurchaseOrderPaymentReadModel.cs`

```csharp
public class PurchaseOrderPaymentReadModel
{
    public Guid          Id              { get; set; }
    public Guid          StoreId         { get; set; }
    public Guid          PurchaseOrderId { get; set; }
    public decimal       Amount          { get; set; }
    public string        Method          { get; set; } = default!;
    public string?       Notes           { get; set; }
    public string        PaidBy          { get; set; } = default!;
    public DateTime      PaidAt          { get; set; }
}
```

### 4.3 Projection Update

Update `PurchaseOrderProjection` untuk menangani:
- `PurchaseOrderPaymentRecorded` → update `TotalPaid` di read model + insert ke `PurchaseOrderPaymentReadModel`
- `PurchaseOrderReceived` → recalculate `TotalCost` (NetReceivedQty × UnitCost tiap line)

### 4.4 Query Repository

Tambah method ke `PurchaseOrderQueryRepository`:

```csharp
// Untuk halaman Hutang Supplier
Task<IReadOnlyList<PurchaseOrderReadModel>> GetOutstandingAsync(
    Guid? supplierId = null, bool overdueOnly = false, CancellationToken ct = default);

// Untuk riwayat pembayaran satu PO
Task<IReadOnlyList<PurchaseOrderPaymentReadModel>> GetPaymentsAsync(
    Guid purchaseOrderId, CancellationToken ct = default);
```

### 4.5 EF Core

```
dotnet ef migrations add AddPurchaseOrderPayment --project Materia.Infrastructure --startup-project Materia.WebApi
```

---

## Fase 5 — API

### Endpoint baru

```
POST /api/purchase-orders/{id}/payments
→ Catat pembayaran hutang ke supplier

GET /api/purchase-orders/{id}/payments
→ Riwayat pembayaran satu PO

GET /api/hutang-supplier
→ List PO dengan outstanding > 0 (nama alias friendly untuk UI)
   Query params: supplierId?, overdueOnly?
```

**Response GET `/api/hutang-supplier`:**

```json
{
  "items": [
    {
      "purchaseOrderId": "...",
      "supplierId": "...",
      "supplierName": "CV Sumber Jaya",
      "createdAt": "2026-06-01",
      "receivedAt": "2026-06-05",
      "paymentDueDate": "2026-07-05",
      "isOverdue": true,
      "totalCost": 5000000,
      "totalPaid": 2000000,
      "outstandingAmount": 3000000
    }
  ],
  "totalOutstanding": 3000000
}
```

Authorization: `Admin`, `SuperAdmin`.

---

## Fase 6 — WebUi

### 6.1 Halaman Hutang Supplier

**File baru:** `Materia.WebUi/Components/Pages/Purchasing/HutangSupplier.razor`

- Tabel: Supplier | No PO | Tgl Terima | Jatuh Tempo | Total | Sudah Bayar | Sisa | Status
- Badge merah "Jatuh Tempo" jika `isOverdue == true`
- Filter: semua supplier / pilih supplier, tampilkan yang sudah lunas / hanya yang belum

### 6.2 Panel Bayar di PurchaseOrderDetail

Di `PurchaseOrderDetail.razor`, tambah section "Pembayaran":
- Tampilkan `TotalCost`, `TotalPaid`, `OutstandingAmount`
- Tombol "Catat Pembayaran" → dialog dengan input amount + metode + catatan
- Riwayat pembayaran (tabel kecil)

### 6.3 Nav Menu

Tambah item "Hutang Supplier" di nav menu (bagian Purchasing), role `Admin`+.

---

## Checklist Eksekusi

```
[ ] Fase 1 — Domain
    [ ] PurchaseOrderPaymentRecorded event
    [ ] TotalPaid property + OutstandingDebt computed di PurchaseOrder
    [ ] RecordPayment() method
    [ ] Apply case

[ ] Fase 2 — Tests
    [ ] PurchaseOrderPaymentTests (semua skenario)

[ ] Fase 3 — Application
    [ ] RecordPurchaseOrderPaymentCommand
    [ ] RecordPurchaseOrderPaymentCommandValidator
    [ ] RecordPurchaseOrderPaymentCommandHandler

[ ] Fase 4 — Infrastructure
    [ ] Update PurchaseOrderReadModel (TotalCost, TotalPaid)
    [ ] PurchaseOrderPaymentReadModel
    [ ] Update PurchaseOrderProjection
    [ ] Update PurchaseOrderQueryRepository
    [ ] EF Core migration

[ ] Fase 5 — API
    [ ] POST /api/purchase-orders/{id}/payments
    [ ] GET /api/purchase-orders/{id}/payments
    [ ] GET /api/hutang-supplier

[ ] Fase 6 — WebUi
    [ ] HutangSupplier.razor halaman list
    [ ] Panel pembayaran di PurchaseOrderDetail
    [ ] Nav menu entry
```

---

## Aturan Bisnis Penting

1. Pembayaran hanya bisa dicatat setelah PO minimal `PartiallyReceived` — tidak bisa bayar sebelum barang datang.
2. Total pembayaran tidak boleh melebihi `TotalCost` (sum NetReceivedQty × UnitCost).
3. Pembayaran parsial diperbolehkan (bayar sebagian, sisanya jadi outstanding).
4. Jika PO di-cancel sebelum diterima → `TotalCost = 0`, tidak ada hutang.
5. Retur pembelian (`PurchaseOrderReturned`) mengurangi `NetReceivedQty` → `TotalCost` ikut turun. Projection harus recalculate `TotalCost` tiap ada event receipt atau return.

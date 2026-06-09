namespace Materia.WebUi.Services;

// PaymentMethod is defined in SaleModels.cs — reused here, not redefined.

/// <summary>
/// Indonesian labels for PaymentMethod, used in the Receivable payment screen.
/// Mirrors the ordinal values of the PaymentMethod enum in SaleModels.cs.
/// </summary>
public static class PaymentMethodLabels
{
    public static string Label(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash         => "Tunai",
        PaymentMethod.BankTransfer => "Transfer Bank",
        PaymentMethod.QRIS         => "QRIS",
        PaymentMethod.Debit        => "Kartu Debit",
        PaymentMethod.Credit       => "Kartu Kredit",
        _                          => method.ToString(),
    };

    public static IReadOnlyList<PaymentMethod> All { get; } =
        [PaymentMethod.Cash, PaymentMethod.BankTransfer, PaymentMethod.QRIS,
         PaymentMethod.Debit, PaymentMethod.Credit];
}

public record ReceivableSummaryDto(
    Guid    CustomerId,
    string  Name,
    string  Phone,
    decimal OutstandingDebt);

public record PagedReceivablesResult(
    List<ReceivableSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record ReceivableAllocationDto(
    Guid    SaleId,
    string  ReferenceNo,
    decimal AppliedAmount,
    decimal RemainingAfter);

public record RecordPaymentResult(
    Guid                        PaymentId,
    decimal                     NewBalance,
    List<ReceivableAllocationDto> Allocations);

using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Financials;

public record ChangeFundDepositDto(
    Guid     Id,
    decimal  Amount,
    string   Source,
    string?  SourceReferenceNo,
    string?  Notes,
    string   RecordedBy,
    DateTime RecordedAt);

public record ChangeFundWithdrawalDto(
    Guid     Id,
    decimal  Amount,
    string   Reason,
    string   RecordedBy,
    DateTime RecordedAt);

public interface IChangeFundQueryRepository
{
    Task<PagedResult<ChangeFundDepositDto>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<PagedResult<ChangeFundWithdrawalDto>> GetPagedWithdrawalsAsync(
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Available change fund balance for the current store:
    /// SUM(deposits) − SUM(withdrawals).
    /// </summary>
    Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default);
}

using Materia.Application.DTOs.Inventory;
using Materia.Domain.Financials;

namespace Materia.Application.Contracts.Financials;

public record PettyCashExpenseDto(
    Guid              Id,
    decimal           Amount,
    string            Recipient,
    PettyCashCategory Category,
    string?           ReasonDetail,
    string            Reason,
    string?           Notes,
    string            ReferenceNo,
    string            RecordedBy,
    DateTime          RecordedAt);

public interface IPettyCashQueryRepository
{
    Task<PagedResult<PettyCashExpenseDto>> GetPagedAsync(
        int page, int pageSize, DateTime? from, DateTime? to,
        PettyCashCategory? category, CancellationToken ct = default);

    Task<PettyCashExpenseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

using Materia.Domain.Sales;

namespace Materia.Application.Contracts.Sales;

public interface ISaleReturnRepository
{
    Task SaveAsync(SaleReturn saleReturn, CancellationToken ct = default);
    Task<SaleReturn?> GetByIdAsync(SaleReturnId id, CancellationToken ct = default);
}

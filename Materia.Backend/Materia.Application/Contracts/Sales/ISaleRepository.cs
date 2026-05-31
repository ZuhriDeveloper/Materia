using Materia.Domain.Sales;

namespace Materia.Application.Contracts.Sales;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(SaleId id, CancellationToken ct = default);
    Task SaveAsync(Sale sale, CancellationToken ct = default);
}

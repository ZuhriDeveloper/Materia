using Materia.Domain.Purchasing;

namespace Materia.Application.Contracts.Purchasing;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(SupplierId id, CancellationToken ct = default);
    Task SaveAsync(Supplier supplier, CancellationToken ct = default);
}

namespace Materia.Application.Contracts.Sales;

public interface IReferenceNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}

namespace Materia.Application.Contracts.Financials;

/// <summary>Generates the human-readable reference for a petty cash entry, e.g. KK-20260609-0001.</summary>
public interface IPettyCashReferenceGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}

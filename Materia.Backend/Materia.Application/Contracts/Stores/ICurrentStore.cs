namespace Materia.Application.Contracts.Stores;

/// <summary>
/// Resolves the store (tenant) the current operation belongs to. In a request this
/// comes from the <c>storeId</c> JWT claim; in background/seed work it comes from an
/// ambient scope. A platform <c>SuperAdmin</c> has no single store.
/// </summary>
public interface ICurrentStore
{
    /// <summary>The current store. Throws when there is no store scope (write path).</summary>
    Guid StoreId { get; }

    /// <summary>The current store, or <c>null</c> for a platform admin / unscoped context (read + filter path).</summary>
    Guid? StoreIdOrNull { get; }

    /// <summary>True when the caller is a platform-level admin not bound to a single store.</summary>
    bool IsPlatformAdmin { get; }
}

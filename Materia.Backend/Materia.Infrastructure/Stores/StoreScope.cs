namespace Materia.Infrastructure.Stores;

/// <summary>
/// Ambient store override for non-HTTP contexts (seeding, background jobs) where there is
/// no <c>storeId</c> JWT claim. <see cref="CurrentStore"/> consults this before the claim.
/// Usage:
/// <code>using (StoreScope.Begin(storeId)) { /* store-scoped work */ }</code>
/// </summary>
public static class StoreScope
{
    private static readonly AsyncLocal<Guid?> Ambient = new();

    public static Guid? Current => Ambient.Value;

    public static IDisposable Begin(Guid storeId)
    {
        var previous = Ambient.Value;
        Ambient.Value = storeId;
        return new Restore(previous);
    }

    private sealed class Restore(Guid? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }
}

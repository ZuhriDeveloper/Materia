namespace Materia.Infrastructure.Stores;

/// <summary>
/// Well-known constants shared across the multi-tenant plumbing — notably between the
/// <c>AddMultiStore</c> migration (which backfills the default store) and the runtime
/// seeder (which must reference the same id).
/// </summary>
public static class StoreConstants
{
    /// <summary>
    /// Id of the default store ("Toko 1") that existing catalog data is stamped with at
    /// rollout. MUST stay in sync with the value hard-coded in the AddMultiStore migration.
    /// </summary>
    public static readonly Guid DefaultStoreId = new("00000000-0000-0000-0000-000000000001");

    public const string DefaultStoreName = "Toko 1";
    public const string DefaultStoreCode = "S1";

    /// <summary>Platform-level role that manages stores and provisions store-scoped users.</summary>
    public const string SuperAdminRole = "SuperAdmin";

    /// <summary>JWT claim carrying the store a user belongs to.</summary>
    public const string StoreClaimType = "storeId";
}

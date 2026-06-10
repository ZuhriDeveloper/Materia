using Materia.Application.Contracts.Stores;
using Microsoft.AspNetCore.Http;

namespace Materia.Infrastructure.Stores;

/// <summary>
/// Resolves the current store from, in order: (1) an ambient <see cref="StoreScope"/>
/// override (seeders / background work), then (2) the <c>storeId</c> claim on the
/// authenticated request. A platform <c>SuperAdmin</c> has neither and is unscoped.
/// </summary>
public sealed class CurrentStore(IHttpContextAccessor accessor) : ICurrentStore
{
    public Guid? StoreIdOrNull
    {
        get
        {
            if (StoreScope.Current is Guid ambient)
                return ambient;

            var raw = accessor.HttpContext?.User.FindFirst(StoreConstants.StoreClaimType)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public Guid StoreId =>
        StoreIdOrNull ?? throw new InvalidOperationException(
            "This operation requires a store-scoped user, but no store is in scope.");

    public bool IsPlatformAdmin =>
        accessor.HttpContext?.User.IsInRole(StoreConstants.SuperAdminRole) ?? false;
}

using Materia.Application.Contracts.Stores;
using Materia.Application.DTOs.Stores;
using Materia.Domain.Common;

namespace Materia.Application.Queries.Stores;

public record GetMyStoreQuery;

public class GetMyStoreQueryHandler(
    IStoreQueryRepository queryRepository,
    IStoreLogoRepository logoRepository,
    ICurrentStore currentStore)
{
    public async Task<StoreProfileDto> HandleAsync(GetMyStoreQuery query, CancellationToken ct = default)
    {
        var storeId = currentStore.StoreId;

        var profile = await queryRepository.GetProfileByIdAsync(storeId, ct)
            ?? throw new DomainException($"Store '{storeId}' not found.");

        var hasLogo = await logoRepository.ExistsAsync(storeId, ct);

        return new StoreProfileDto(
            profile.Id,
            profile.Name,
            profile.Code,
            profile.IsActive,
            profile.Address,
            profile.Phone,
            profile.MaxDeliveryDistanceKm,
            hasLogo);
    }
}

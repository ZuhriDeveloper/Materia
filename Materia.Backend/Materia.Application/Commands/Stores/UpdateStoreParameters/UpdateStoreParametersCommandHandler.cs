using Materia.Application.Contracts.Stores;
using Materia.Domain.Common;
using Materia.Domain.Stores;

namespace Materia.Application.Commands.Stores.UpdateStoreParameters;

public class UpdateStoreParametersCommandHandler(
    IStoreRepository repository,
    ICurrentStore currentStore)
{
    public async Task HandleAsync(UpdateStoreParametersCommand command, CancellationToken ct = default)
    {
        var storeId = StoreId.From(currentStore.StoreId);
        var store = await repository.GetByIdAsync(storeId, ct)
            ?? throw new DomainException($"Store '{storeId}' not found.");

        store.UpdateParameters(command.Address, command.Phone, command.MaxDeliveryDistanceKm, command.UpdatedBy);
        await repository.SaveAsync(store, ct);
    }
}

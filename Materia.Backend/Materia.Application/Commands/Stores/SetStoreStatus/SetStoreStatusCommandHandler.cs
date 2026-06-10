using Materia.Application.Contracts.Stores;
using Materia.Domain.Common;
using Materia.Domain.Stores;

namespace Materia.Application.Commands.Stores.SetStoreStatus;

public class SetStoreStatusCommandHandler(IStoreRepository repository)
{
    public async Task HandleAsync(SetStoreStatusCommand command, CancellationToken ct = default)
    {
        var store = await repository.GetByIdAsync(StoreId.From(command.StoreId), ct)
            ?? throw new DomainException($"Store '{command.StoreId}' not found.");

        if (command.IsActive)
            store.Activate(command.UpdatedBy);
        else
            store.Deactivate(command.UpdatedBy);

        await repository.SaveAsync(store, ct);
    }
}

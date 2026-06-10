using Materia.Application.Contracts.Stores;
using Materia.Domain.Common;
using Materia.Domain.Stores;

namespace Materia.Application.Commands.Stores.RenameStore;

public class RenameStoreCommandHandler(IStoreRepository repository)
{
    public async Task HandleAsync(RenameStoreCommand command, CancellationToken ct = default)
    {
        var store = await repository.GetByIdAsync(StoreId.From(command.StoreId), ct)
            ?? throw new DomainException($"Store '{command.StoreId}' not found.");

        store.Rename(command.Name, command.RenamedBy);
        await repository.SaveAsync(store, ct);
    }
}

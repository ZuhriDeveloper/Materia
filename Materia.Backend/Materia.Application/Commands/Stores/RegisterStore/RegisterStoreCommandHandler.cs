using Materia.Application.Contracts.Stores;
using Materia.Domain.Common;
using Materia.Domain.Stores;

namespace Materia.Application.Commands.Stores.RegisterStore;

public class RegisterStoreCommandHandler(
    IStoreRepository repository,
    IStoreQueryRepository queryRepository)
{
    public async Task<Guid> HandleAsync(RegisterStoreCommand command, CancellationToken ct = default)
    {
        if (await queryRepository.CodeExistsAsync(command.Code.Trim(), ct))
            throw new DomainException($"A store with code '{command.Code}' already exists.");

        var store = Store.Register(command.Name, command.Code, command.RegisteredBy);
        await repository.SaveAsync(store, ct);
        return store.Id.Value;
    }
}

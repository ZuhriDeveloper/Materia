using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.SetCategoryStatus;

public class SetCategoryStatusCommandHandler(ICategoryRepository repository)
{
    public async Task HandleAsync(SetCategoryStatusCommand command, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(CategoryId.From(command.CategoryId), ct)
            ?? throw new DomainException($"Category '{command.CategoryId}' not found.");

        if (command.IsActive)
            category.Activate(command.UpdatedBy);
        else
            category.Deactivate(command.UpdatedBy);

        await repository.SaveAsync(category, ct);
    }
}

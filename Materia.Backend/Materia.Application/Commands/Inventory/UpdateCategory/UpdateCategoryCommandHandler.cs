using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.UpdateCategory;

public class UpdateCategoryCommandHandler(ICategoryRepository repository)
{
    public async Task HandleAsync(UpdateCategoryCommand command, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(CategoryId.From(command.CategoryId), ct)
            ?? throw new DomainException($"Category '{command.CategoryId}' not found.");

        category.Update(command.Name, command.Description, command.UpdatedBy);
        await repository.SaveAsync(category, ct);
    }
}

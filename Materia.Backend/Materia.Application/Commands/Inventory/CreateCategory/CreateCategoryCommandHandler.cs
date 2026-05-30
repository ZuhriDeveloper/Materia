using Materia.Application.Contracts.Inventory;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.CreateCategory;

public class CreateCategoryCommandHandler(ICategoryRepository repository)
{
    public async Task<Guid> HandleAsync(CreateCategoryCommand command, CancellationToken ct = default)
    {
        var category = Category.Create(command.Name, command.Description, command.CreatedBy);
        await repository.SaveAsync(category, ct);
        return category.Id.Value;
    }
}

using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;

namespace Materia.Domain.Inventory;

public class Category : AggregateRoot<CategoryId>
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private Category() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Category Create(string name, string? description, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name cannot be empty.");

        var category = new Category();
        category.Raise(new CategoryCreated(
            CategoryId.New(),
            name.Trim(),
            description?.Trim(),
            createdBy,
            DateTime.UtcNow));
        return category;
    }

    public static Category Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var category = new Category();
        category.Load(events);
        return category;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Update(string name, string? description, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name cannot be empty.");

        if (Name != name.Trim())
            Raise(new CategoryNameUpdated(Id, name.Trim(), updatedBy, DateTime.UtcNow));

        if (Description != description?.Trim())
            Raise(new CategoryDescriptionUpdated(Id, description?.Trim(), updatedBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case CategoryCreated e:
                Id = e.CategoryId;
                Name = e.Name;
                Description = e.Description;
                IsActive = true;
                break;

            case CategoryNameUpdated e:
                Name = e.Name;
                break;

            case CategoryDescriptionUpdated e:
                Description = e.Description;
                break;
        }
    }
}

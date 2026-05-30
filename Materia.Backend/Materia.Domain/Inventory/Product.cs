using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;

namespace Materia.Domain.Inventory;

public class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public UnitName BaseUnit { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private readonly List<CategoryId> _categoryIds = [];
    private readonly List<UnitConversion> _unitConversions = [];

    public IReadOnlyList<CategoryId> CategoryIds => _categoryIds.AsReadOnly();
    public IReadOnlyList<UnitConversion> UnitConversions => _unitConversions.AsReadOnly();

    // Required for Reconstitute
    private Product() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Product Create(string name, string? description, UnitName baseUnit, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");

        var product = new Product();
        product.Raise(new ProductCreated(
            ProductId.New(),
            name.Trim(),
            description?.Trim(),
            baseUnit.Value,
            createdBy,
            DateTime.UtcNow));
        return product;
    }

    public static Product Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var product = new Product();
        product.Load(events);
        return product;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Update(string name, string? description, string updatedBy)
    {
        if (!IsActive)
            throw new DomainException("Cannot update an inactive product.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");

        if (Name != name.Trim())
            Raise(new ProductNameUpdated(Id, name.Trim(), updatedBy, DateTime.UtcNow));

        if (Description != description?.Trim())
            Raise(new ProductDescriptionUpdated(Id, description?.Trim(), updatedBy, DateTime.UtcNow));
    }

    public void Deactivate(string deactivatedBy)
    {
        if (!IsActive)
            throw new DomainException("Product is already inactive.");

        Raise(new ProductDeactivated(Id, deactivatedBy, DateTime.UtcNow));
    }

    public void Activate(string activatedBy)
    {
        if (IsActive)
            throw new DomainException("Product is already active.");

        Raise(new ProductActivated(Id, activatedBy, DateTime.UtcNow));
    }

    public void AssignCategory(CategoryId categoryId, string updatedBy)
    {
        if (_categoryIds.Contains(categoryId)) return; // idempotent

        Raise(new ProductCategoryAssigned(Id, categoryId, updatedBy, DateTime.UtcNow));
    }

    public void RemoveCategory(CategoryId categoryId, string updatedBy)
    {
        if (!_categoryIds.Contains(categoryId)) return; // idempotent

        Raise(new ProductCategoryRemoved(Id, categoryId, updatedBy, DateTime.UtcNow));
    }

    public void AddUnitConversion(UnitConversion conversion, string updatedBy)
    {
        if (_unitConversions.Any(c => c.ToUnit == conversion.ToUnit))
            throw new DomainException($"A conversion to unit '{conversion.ToUnit.Value}' already exists.");

        Raise(new ProductUnitConversionAdded(
            Id,
            conversion.FromUnit.Value,
            conversion.ToUnit.Value,
            conversion.Factor,
            updatedBy,
            DateTime.UtcNow));
    }

    public void RemoveUnitConversion(UnitName toUnit, string updatedBy)
    {
        if (!_unitConversions.Any(c => c.ToUnit == toUnit)) return; // idempotent

        Raise(new ProductUnitConversionRemoved(Id, toUnit.Value, updatedBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case ProductCreated e:
                Id = e.ProductId;
                Name = e.Name;
                Description = e.Description;
                BaseUnit = new UnitName(e.BaseUnit);
                IsActive = true;
                break;

            case ProductNameUpdated e:
                Name = e.Name;
                break;

            case ProductDescriptionUpdated e:
                Description = e.Description;
                break;

            case ProductDeactivated:
                IsActive = false;
                break;

            case ProductActivated:
                IsActive = true;
                break;

            case ProductCategoryAssigned e:
                _categoryIds.Add(e.CategoryId);
                break;

            case ProductCategoryRemoved e:
                _categoryIds.Remove(e.CategoryId);
                break;

            case ProductUnitConversionAdded e:
                _unitConversions.Add(new UnitConversion(
                    new UnitName(e.FromUnit),
                    new UnitName(e.ToUnit),
                    e.Factor));
                break;

            case ProductUnitConversionRemoved e:
                _unitConversions.RemoveAll(c => c.ToUnit.Value == e.ToUnit);
                break;
        }
    }
}

using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;

namespace Materia.Domain.Inventory;

public class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public UnitName BaseUnit { get; private set; } = default!;
    public decimal SalePrice { get; private set; }
    public string? Barcode { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<CategoryId> _categoryIds = [];
    private readonly List<UnitConversion> _unitConversions = [];
    private readonly List<ColorVariant> _colorVariants = [];

    public IReadOnlyList<CategoryId> CategoryIds => _categoryIds.AsReadOnly();
    public IReadOnlyList<UnitConversion> UnitConversions => _unitConversions.AsReadOnly();
    public IReadOnlyList<ColorVariant> ColorVariants => _colorVariants.AsReadOnly();

    // Required for Reconstitute
    private Product() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Product Create(
        string name, string? description, UnitName baseUnit, string createdBy,
        decimal salePrice = 0m, string? barcode = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");
        if (salePrice < 0)
            throw new DomainException("Sale price cannot be negative.");

        var product = new Product();
        product.Raise(new ProductCreated(
            ProductId.New(),
            name.Trim(),
            description?.Trim(),
            baseUnit.Value,
            createdBy,
            DateTime.UtcNow,
            salePrice,
            NormalizeBarcode(barcode)));
        return product;
    }

    public static Product Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var product = new Product();
        product.Load(events);
        return product;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Update(
        string name, string? description, string updatedBy,
        decimal salePrice = 0m, string? barcode = null)
    {
        if (!IsActive)
            throw new DomainException("Cannot update an inactive product.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");
        if (salePrice < 0)
            throw new DomainException("Sale price cannot be negative.");

        if (Name != name.Trim())
            Raise(new ProductNameUpdated(Id, name.Trim(), updatedBy, DateTime.UtcNow));

        if (Description != description?.Trim())
            Raise(new ProductDescriptionUpdated(Id, description?.Trim(), updatedBy, DateTime.UtcNow));

        if (SalePrice != salePrice)
            Raise(new ProductSalePriceChanged(Id, salePrice, updatedBy, DateTime.UtcNow));

        var normalizedBarcode = NormalizeBarcode(barcode);
        if (Barcode != normalizedBarcode)
            Raise(new ProductBarcodeChanged(Id, normalizedBarcode, updatedBy, DateTime.UtcNow));
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
            DateTime.UtcNow,
            conversion.SalePrice));
    }

    public void RemoveUnitConversion(UnitName toUnit, string updatedBy)
    {
        if (!_unitConversions.Any(c => c.ToUnit == toUnit)) return; // idempotent

        Raise(new ProductUnitConversionRemoved(Id, toUnit.Value, updatedBy, DateTime.UtcNow));
    }

    // ── Color Variants ────────────────────────────────────────────────────────

    public VariantId AddColorVariant(
        Color color, string? barcode, decimal? priceOverride, string updatedBy)
    {
        EnsureVariantPriceValid(priceOverride);
        EnsureColorNameAvailable(color.Name, exclude: null);

        var normalizedBarcode = NormalizeBarcode(barcode);
        EnsureVariantBarcodeAvailable(normalizedBarcode, exclude: null);

        var variantId = VariantId.New();
        Raise(new ProductColorVariantAdded(
            Id, variantId, color.Name, color.Code, normalizedBarcode, priceOverride,
            updatedBy, DateTime.UtcNow));
        return variantId;
    }

    public void UpdateColorVariant(
        VariantId variantId, Color color, string? barcode, decimal? priceOverride, string updatedBy)
    {
        _ = FindVariant(variantId);
        EnsureVariantPriceValid(priceOverride);
        EnsureColorNameAvailable(color.Name, exclude: variantId);

        var normalizedBarcode = NormalizeBarcode(barcode);
        EnsureVariantBarcodeAvailable(normalizedBarcode, exclude: variantId);

        Raise(new ProductColorVariantUpdated(
            Id, variantId, color.Name, color.Code, normalizedBarcode, priceOverride,
            updatedBy, DateTime.UtcNow));
    }

    public void RemoveColorVariant(VariantId variantId, string updatedBy)
    {
        if (_colorVariants.All(v => v.Id != variantId)) return; // idempotent

        Raise(new ProductColorVariantRemoved(Id, variantId, updatedBy, DateTime.UtcNow));
    }

    public void DeactivateColorVariant(VariantId variantId, string updatedBy)
    {
        var variant = FindVariant(variantId);
        if (!variant.IsActive)
            throw new DomainException("Color variant is already inactive.");

        Raise(new ProductColorVariantDeactivated(Id, variantId, updatedBy, DateTime.UtcNow));
    }

    public void ActivateColorVariant(VariantId variantId, string updatedBy)
    {
        var variant = FindVariant(variantId);
        if (variant.IsActive)
            throw new DomainException("Color variant is already active.");

        Raise(new ProductColorVariantActivated(Id, variantId, updatedBy, DateTime.UtcNow));
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
                SalePrice = e.SalePrice;
                Barcode = e.Barcode;
                IsActive = true;
                break;

            case ProductNameUpdated e:
                Name = e.Name;
                break;

            case ProductDescriptionUpdated e:
                Description = e.Description;
                break;

            case ProductSalePriceChanged e:
                SalePrice = e.SalePrice;
                break;

            case ProductBarcodeChanged e:
                Barcode = e.Barcode;
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
                    e.Factor,
                    e.SalePrice));
                break;

            case ProductUnitConversionRemoved e:
                _unitConversions.RemoveAll(c => c.ToUnit.Value == e.ToUnit);
                break;

            case ProductColorVariantAdded e:
                _colorVariants.Add(new ColorVariant(
                    e.VariantId, new Color(e.ColorName, e.ColorCode), e.Barcode, e.PriceOverride));
                break;

            case ProductColorVariantUpdated e:
                FindVariant(e.VariantId)
                    .Update(new Color(e.ColorName, e.ColorCode), e.Barcode, e.PriceOverride);
                break;

            case ProductColorVariantRemoved e:
                _colorVariants.RemoveAll(v => v.Id == e.VariantId);
                break;

            case ProductColorVariantDeactivated e:
                FindVariant(e.VariantId).Deactivate();
                break;

            case ProductColorVariantActivated e:
                FindVariant(e.VariantId).Activate();
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? NormalizeBarcode(string? barcode)
        => string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();

    private ColorVariant FindVariant(VariantId variantId)
        => _colorVariants.FirstOrDefault(v => v.Id == variantId)
           ?? throw new DomainException($"Color variant '{variantId}' not found.");

    private static void EnsureVariantPriceValid(decimal? priceOverride)
    {
        if (priceOverride is < 0)
            throw new DomainException("Variant price override cannot be negative.");
    }

    private void EnsureColorNameAvailable(string colorName, VariantId? exclude)
    {
        if (_colorVariants.Any(v =>
                v.Id != exclude &&
                string.Equals(v.Color.Name, colorName, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"A color variant '{colorName}' already exists.");
    }

    private void EnsureVariantBarcodeAvailable(string? normalizedBarcode, VariantId? exclude)
    {
        if (normalizedBarcode is not null &&
            _colorVariants.Any(v => v.Id != exclude && v.Barcode == normalizedBarcode))
            throw new DomainException($"Barcode '{normalizedBarcode}' is already used by another variant.");
    }
}

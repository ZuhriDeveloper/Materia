using FluentAssertions;
using Materia.Application.Commands.Purchasing.CreatePurchaseOrder;
using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;

namespace Materia.Tests.Purchasing;

public class CreatePurchaseOrderHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeSupplierRepository(Supplier supplier) : ISupplierRepository
    {
        public Task<Supplier?> GetByIdAsync(SupplierId id, CancellationToken ct = default)
            => Task.FromResult(supplier.Id == id ? supplier : null);

        public Task SaveAsync(Supplier s, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePurchaseOrderRepository : IPurchaseOrderRepository
    {
        public PurchaseOrder? Saved { get; private set; }

        public Task<PurchaseOrder?> GetByIdAsync(PurchaseOrderId id, CancellationToken ct = default)
            => Task.FromResult<PurchaseOrder?>(null);

        public Task SaveAsync(PurchaseOrder po, CancellationToken ct = default)
        {
            Saved = po;
            po.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>A supplier whose catalog holds the discounted "pipe" and an undiscounted product.</summary>
    private static (Supplier supplier, ProductId pipe, ProductId other) SupplierWithCatalog()
    {
        var supplier = Supplier.Register("PT Pipa Sentosa", null, null, null, null, "admin");
        var pipe = ProductId.New();
        var other = ProductId.New();

        supplier.SetPurchasePrice(pipe,
            new PurchasePrice(120_000m, "IDR", "batang", DateTime.UtcNow, [12.5m, 7m, 5m]), "admin");
        supplier.SetPurchasePrice(other,
            new PurchasePrice(50_000m, "IDR", "pcs", DateTime.UtcNow), "admin");
        supplier.ClearDomainEvents();
        return (supplier, pipe, other);
    }

    private static (CreatePurchaseOrderCommandHandler handler, FakePurchaseOrderRepository poRepo)
        BuildHandler(Supplier supplier)
    {
        var poRepo = new FakePurchaseOrderRepository();
        var handler = new CreatePurchaseOrderCommandHandler(
            new FakeSupplierRepository(supplier), poRepo);
        return (handler, poRepo);
    }

    // ── Standing-discount auto-fill ─────────────────────────────────────────────

    [Fact]
    public async Task Create_LineWithoutDiscount_FillsFromCatalogStandingDiscount()
    {
        var (supplier, pipe, _) = SupplierWithCatalog();
        var (handler, poRepo) = BuildHandler(supplier);

        await handler.HandleAsync(new CreatePurchaseOrderCommand(
            supplier.Id.Value,
            [new CreatePurchaseOrderLineInput(pipe.Value, 10m)],   // no per-line discount
            "admin"));

        var line = poRepo.Saved!.Lines.Single();
        line.Discounts.Should().Equal(12.5m, 7m, 5m);
        line.ListUnitCost.Should().Be(120_000m);
    }

    [Fact]
    public async Task Create_LineWithExplicitDiscount_OverridesCatalogStandingDiscount()
    {
        var (supplier, pipe, _) = SupplierWithCatalog();
        var (handler, poRepo) = BuildHandler(supplier);

        await handler.HandleAsync(new CreatePurchaseOrderCommand(
            supplier.Id.Value,
            [new CreatePurchaseOrderLineInput(pipe.Value, 10m, Discounts: [20m])],
            "admin"));

        poRepo.Saved!.Lines.Single().Discounts.Should().Equal(20m);
    }

    [Fact]
    public async Task Create_ProductWithoutStandingDiscount_HasNoDiscount()
    {
        // The Rucika/Onda case: a product the supplier sells without a granted discount.
        var (supplier, _, other) = SupplierWithCatalog();
        var (handler, poRepo) = BuildHandler(supplier);

        await handler.HandleAsync(new CreatePurchaseOrderCommand(
            supplier.Id.Value,
            [new CreatePurchaseOrderLineInput(other.Value, 5m)],
            "admin"));

        var line = poRepo.Saved!.Lines.Single();
        line.Discounts.Should().BeEmpty();
        line.UnitCost.Should().Be(50_000m);
    }
}

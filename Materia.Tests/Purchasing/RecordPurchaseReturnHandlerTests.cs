using FluentAssertions;
using Materia.Application.Commands.Purchasing.RecordPurchaseReturn;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;

namespace Materia.Tests.Purchasing;

public class RecordPurchaseReturnHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakePurchaseOrderRepository : IPurchaseOrderRepository
    {
        private PurchaseOrder? _stored;
        public PurchaseOrder? Saved { get; private set; }

        public void Seed(PurchaseOrder po) => _stored = po;

        public Task<PurchaseOrder?> GetByIdAsync(PurchaseOrderId id, CancellationToken ct = default)
            => Task.FromResult(_stored?.Id == id ? _stored : null);

        public Task SaveAsync(PurchaseOrder po, CancellationToken ct = default)
        {
            Saved = po;
            po.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStockRepository : IStockRepository
    {
        private readonly List<Stock> _stored = [];
        public List<Stock> Saved { get; } = [];

        public void Seed(Stock stock) => _stored.Add(stock);

        public Task<Stock?> GetAsync(
            ProductId productId, VariantId? variantId = null, CancellationToken ct = default)
            => Task.FromResult(_stored.FirstOrDefault(s =>
                s.ProductId == productId && NullableVariantEquals(s.VariantId, variantId)));

        public Task SaveAsync(Stock stock, CancellationToken ct = default)
        {
            if (!Saved.Contains(stock)) Saved.Add(stock);
            stock.ClearDomainEvents();
            return Task.CompletedTask;
        }

        private static bool NullableVariantEquals(VariantId? a, VariantId? b)
            => (a is null && b is null) || (a is not null && b is not null && a.Value == b.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (
        RecordPurchaseReturnCommandHandler handler,
        FakePurchaseOrderRepository poRepo,
        FakeStockRepository stockRepo)
    BuildHandler()
    {
        var poRepo = new FakePurchaseOrderRepository();
        var stockRepo = new FakeStockRepository();
        var handler = new RecordPurchaseReturnCommandHandler(poRepo, stockRepo);
        return (handler, poRepo, stockRepo);
    }

    private static PurchaseOrder ReceivedPo(ProductId productId, decimal qty = 10m)
    {
        var po = PurchaseOrder.Create(
            SupplierId.New(), [(productId, qty, 50_000m, "pcs")], "user1");
        po.Confirm("user1");
        po.Receive([(productId, qty)], "warehouse");
        po.ClearDomainEvents();
        return po;
    }

    private static Stock SeededStock(ProductId productId, decimal qty, VariantId? variantId = null)
    {
        var stock = Stock.Initialize(productId, "pcs", "system", variantId);
        if (qty != 0m) stock.Adjust(qty, "seed", "system");
        stock.ClearDomainEvents();
        return stock;
    }

    // ── Per-variant routing ───────────────────────────────────────────────────

    [Fact]
    public async Task Return_WithVariantId_ReducesVariantStock_NotProductLevel()
    {
        var (handler, poRepo, stockRepo) = BuildHandler();
        var productId = ProductId.New();
        var variantId = VariantId.New();
        var po = ReceivedPo(productId);
        poRepo.Seed(po);

        var productStock = SeededStock(productId, 0m);
        var variantStock = SeededStock(productId, 10m, variantId);
        stockRepo.Seed(productStock);
        stockRepo.Seed(variantStock);

        await handler.HandleAsync(new RecordPurchaseReturnCommand(
            po.Id.Value,
            [new ReturnPurchaseOrderLineInput(productId.Value, 3m, variantId.Value)],
            "broken on arrival",
            "warehouse"));

        variantStock.Quantity.Should().Be(7m);
        productStock.Quantity.Should().Be(0m);
    }

    [Fact]
    public async Task Return_WithoutVariantId_ReducesProductLevelStock()
    {
        var (handler, poRepo, stockRepo) = BuildHandler();
        var productId = ProductId.New();
        var po = ReceivedPo(productId);
        poRepo.Seed(po);

        var productStock = SeededStock(productId, 10m);
        var variantStock = SeededStock(productId, 10m, VariantId.New());
        stockRepo.Seed(productStock);
        stockRepo.Seed(variantStock);

        await handler.HandleAsync(new RecordPurchaseReturnCommand(
            po.Id.Value,
            [new ReturnPurchaseOrderLineInput(productId.Value, 3m)],
            "broken on arrival",
            "warehouse"));

        productStock.Quantity.Should().Be(7m);
        variantStock.Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task Return_WithUnknownVariant_ThrowsDomainException()
    {
        var (handler, poRepo, stockRepo) = BuildHandler();
        var productId = ProductId.New();
        var po = ReceivedPo(productId);
        poRepo.Seed(po);
        stockRepo.Seed(SeededStock(productId, 10m)); // only product-level; no variant bucket

        Func<Task> act = () => handler.HandleAsync(new RecordPurchaseReturnCommand(
            po.Id.Value,
            [new ReturnPurchaseOrderLineInput(productId.Value, 3m, VariantId.New().Value)],
            "broken on arrival",
            "warehouse"));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*variant*");
    }
}

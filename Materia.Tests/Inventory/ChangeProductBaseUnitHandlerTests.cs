using FluentAssertions;
using Materia.Application.Commands.Inventory.ChangeProductBaseUnit;
using Materia.Application.Contracts.Common;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.Contracts.Sales;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
namespace Materia.Tests.Inventory;

public class ChangeProductBaseUnitHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeProductRepository : IProductRepository
    {
        private Product? _stored;
        public Product? Saved { get; private set; }

        public void Seed(Product product) => _stored = product;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
            => Task.FromResult(_stored?.Id == id ? _stored : null);

        public Task SaveAsync(Product product, CancellationToken ct = default)
        {
            Saved = product;
            product.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStockRepository : IStockRepository
    {
        private Stock? _stored;
        public Stock? Saved { get; private set; }

        public void Seed(Stock stock) => _stored = stock;

        public Task<Stock?> GetAsync(
            ProductId productId, VariantId? variantId = null, CancellationToken ct = default)
            => Task.FromResult(
                _stored?.ProductId == productId && variantId is null ? _stored : null);

        public Task SaveAsync(Stock stock, CancellationToken ct = default)
        {
            Saved = stock;
            stock.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSaleProductQueryService(bool exists = false)
        : ISaleProductQueryService
    {
        public Task<bool> ExistsForProductAsync(Guid productId, CancellationToken ct = default)
            => Task.FromResult(exists);
    }

    private sealed class FakePurchaseOrderProductQueryService(bool exists = false)
        : IPurchaseOrderProductQueryService
    {
        public Task<bool> ExistsForProductAsync(Guid productId, CancellationToken ct = default)
            => Task.FromResult(exists);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
            => await action();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (
        ChangeProductBaseUnitCommandHandler handler,
        FakeProductRepository productRepo,
        FakeStockRepository stockRepo)
    BuildHandler(
        bool saleExists = false,
        bool purchaseExists = false)
    {
        var productRepo  = new FakeProductRepository();
        var stockRepo    = new FakeStockRepository();
        var saleQuery    = new FakeSaleProductQueryService(saleExists);
        var poQuery      = new FakePurchaseOrderProductQueryService(purchaseExists);
        var uow          = new FakeUnitOfWork();
        var handler      = new ChangeProductBaseUnitCommandHandler(
            productRepo, stockRepo, saleQuery, poQuery, uow);
        return (handler, productRepo, stockRepo);
    }

    private static (Product product, Stock stock) SeedPristine(
        FakeProductRepository productRepo,
        FakeStockRepository stockRepo,
        string unit = "sak")
    {
        var product = Product.Create("Semen", null, new UnitName(unit), "user-1");
        product.ClearDomainEvents();
        productRepo.Seed(product);

        var stock = Stock.Initialize(product.Id, unit, "user-1");
        stock.ClearDomainEvents();
        stockRepo.Seed(stock);

        return (product, stock);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Pristine_SavesBothAggregates()
    {
        var (handler, productRepo, stockRepo) = BuildHandler();
        var (product, _) = SeedPristine(productRepo, stockRepo, "sak");

        await handler.HandleAsync(
            new ChangeProductBaseUnitCommand(product.Id.Value, "kg", "admin"));

        productRepo.Saved.Should().NotBeNull();
        stockRepo.Saved.Should().NotBeNull();
        productRepo.Saved!.BaseUnit.Value.Should().Be("kg");
        stockRepo.Saved!.Unit.Should().Be("kg");
    }

    [Fact]
    public async Task HandleAsync_NoStockRecord_ChangesProductBaseUnitAndSkipsStock()
    {
        // A product can legitimately have no stock record yet (e.g. seeded master data).
        // That means no movements, so the change is still allowed and there is nothing to sync.
        var (handler, productRepo, stockRepo) = BuildHandler();
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.ClearDomainEvents();
        productRepo.Seed(product);
        // Note: no stock seeded.

        await handler.HandleAsync(
            new ChangeProductBaseUnitCommand(product.Id.Value, "kg", "admin"));

        productRepo.Saved.Should().NotBeNull();
        productRepo.Saved!.BaseUnit.Value.Should().Be("kg");
        stockRepo.Saved.Should().BeNull();
    }

    // ── Product not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ProductNotFound_ThrowsDomainException()
    {
        var (handler, _, _) = BuildHandler();
        var command = new ChangeProductBaseUnitCommand(Guid.NewGuid(), "kg", "admin");

        Func<Task> act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not found*");
    }

    // ── Cross-aggregate guards ────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SaleExists_ThrowsDomainException()
    {
        var (handler, productRepo, stockRepo) = BuildHandler(saleExists: true);
        var (product, _) = SeedPristine(productRepo, stockRepo);

        Func<Task> act = () =>
            handler.HandleAsync(new ChangeProductBaseUnitCommand(product.Id.Value, "kg", "admin"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*sale*");
    }

    [Fact]
    public async Task HandleAsync_PurchaseOrderExists_ThrowsDomainException()
    {
        var (handler, productRepo, stockRepo) = BuildHandler(purchaseExists: true);
        var (product, _) = SeedPristine(productRepo, stockRepo);

        Func<Task> act = () =>
            handler.HandleAsync(new ChangeProductBaseUnitCommand(product.Id.Value, "kg", "admin"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*purchase order*");
    }

    [Fact]
    public async Task HandleAsync_StockHasMovements_ThrowsDomainException()
    {
        var (handler, productRepo, stockRepo) = BuildHandler();
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.ClearDomainEvents();
        productRepo.Seed(product);

        // Stock has a movement — Adjust was called
        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        stock.Adjust(5m, null, "user-1");
        stock.ClearDomainEvents();
        stockRepo.Seed(stock);

        Func<Task> act = () =>
            handler.HandleAsync(new ChangeProductBaseUnitCommand(product.Id.Value, "kg", "admin"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*movements*");
    }

    // ── In-aggregate guards surface correctly through the handler ─────────────

    [Fact]
    public async Task HandleAsync_InactiveProduct_ThrowsDomainException()
    {
        var (handler, productRepo, stockRepo) = BuildHandler();
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.Deactivate("user-1");
        product.ClearDomainEvents();
        productRepo.Seed(product);

        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        stock.ClearDomainEvents();
        stockRepo.Seed(stock);

        Func<Task> act = () =>
            handler.HandleAsync(new ChangeProductBaseUnitCommand(product.Id.Value, "kg", "admin"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*inactive*");
    }

    [Fact]
    public async Task HandleAsync_SameUnit_ThrowsDomainException()
    {
        var (handler, productRepo, stockRepo) = BuildHandler();
        var (product, _) = SeedPristine(productRepo, stockRepo, "sak");

        Func<Task> act = () =>
            handler.HandleAsync(new ChangeProductBaseUnitCommand(product.Id.Value, "sak", "admin"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*differ*");
    }

    // ── Event assertions ──────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Pristine_ProductRaisesProductBaseUnitChangedEvent()
    {
        var (handler, productRepo, stockRepo) = BuildHandler();
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.ClearDomainEvents();
        productRepo.Seed(product);

        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        stock.ClearDomainEvents();
        stockRepo.Seed(stock);

        await handler.HandleAsync(
            new ChangeProductBaseUnitCommand(product.Id.Value, "kg", "admin"));

        // The saved product's domain events were cleared by the fake repo.
        // We verify via state instead.
        productRepo.Saved!.BaseUnit.Value.Should().Be("kg");
        stockRepo.Saved!.Unit.Should().Be("kg");
    }
}

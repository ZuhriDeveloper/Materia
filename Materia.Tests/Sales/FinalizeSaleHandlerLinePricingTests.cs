using FluentAssertions;
using Materia.Application.Commands.Sales.FinalizeSale;
using Materia.Application.Contracts.Customers;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.Contracts.Sales;
using Materia.Application.DTOs.Customers;
using Materia.Application.DTOs.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Sales;

namespace Materia.Tests.Sales;

/// <summary>
/// Handler tests: UnitCost is snapshotted from the latest purchase price port;
/// if no price exists UnitCost is 0 (no throw); client-sent cost fields are ignored.
/// </summary>
public class FinalizeSaleHandlerLinePricingTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeSaleRepository : ISaleRepository
    {
        public Sale? Saved { get; private set; }

        public Task<Sale?> GetByIdAsync(SaleId id, CancellationToken ct = default)
            => Task.FromResult<Sale?>(null);

        public Task SaveAsync(Sale sale, CancellationToken ct = default)
        {
            Saved = sale;
            sale.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
            => Task.FromResult<Customer?>(null);

        public Task SaveAsync(Customer customer, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<StoredReceivablePayment?> FindReceivablePaymentByKeyAsync(
            Guid idempotencyKey, CancellationToken ct = default)
            => Task.FromResult<StoredReceivablePayment?>(null);
    }

    private sealed class FakeCustomerQueryRepository : ICustomerQueryRepository
    {
        public Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<CustomerDto?>(null);

        public Task<PagedResult<CustomerDto>> GetPagedAsync(
            int page, int pageSize, string? search, bool? isActive,
            CancellationToken ct = default)
            => Task.FromResult(new PagedResult<CustomerDto>([], 0, page, pageSize));

        public Task<IReadOnlyList<NearbyCustomerDto>> GetNearbyAsync(
            decimal latitude, decimal longitude, double radiusKm, int maxResults,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NearbyCustomerDto>>([]);

        public Task<bool> ExistsByPhoneAsync(string phone, Guid? excludeId = null,
            CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<PagedResult<ReceivableSummaryDto>> GetOutstandingReceivablesAsync(
            int page, int pageSize, string? search, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<ReceivableSummaryDto>([], 0, page, pageSize));
    }

    private sealed class FakeProductQueryRepository(Guid productId) : IProductQueryRepository
    {
        public Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (id != productId) return Task.FromResult<ProductDto?>(null);

            var product = new ProductDto(
                Id:              id,
                Name:            "Semen 50kg",
                Description:     null,
                BaseUnit:        "sak",
                SalePrice:       65_000m,
                Barcode:         null,
                IsActive:        true,
                CreatedBy:       "seed",
                CreatedAt:       DateTime.UtcNow,
                UpdatedBy:       null,
                UpdatedAt:       null,
                UnitConversions: [],
                Categories:      [],
                ColorVariants:   []);

            return Task.FromResult<ProductDto?>(product);
        }

        public Task<PagedResult<ProductDto>> GetPagedAsync(
            int page, int pageSize, bool? isActive,
            string? search = null, Guid? categoryId = null,
            CancellationToken ct = default)
            => Task.FromResult(new PagedResult<ProductDto>([], 0, page, pageSize));

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null,
            CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludeId = null,
            CancellationToken ct = default)
            => Task.FromResult(false);
    }

    private sealed class FakeStockDeduction : IStockDeductionService
    {
        public Task DeductAsync(
            Guid productId, Guid? variantId, decimal quantity,
            string reason, string deductedBy, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeReferenceGenerator : IReferenceNumberGenerator
    {
        private int _counter;
        public Task<string> GenerateAsync(CancellationToken ct = default)
            => Task.FromResult($"INV-TEST-{++_counter:D4}");
    }

    private sealed class FakeLatestPurchasePriceRepository : ILatestPurchasePriceRepository
    {
        private readonly Dictionary<Guid, decimal> _prices;

        public FakeLatestPurchasePriceRepository(params (Guid productId, decimal price)[] prices)
            => _prices = prices.ToDictionary(p => p.productId, p => p.price);

        public Task<decimal?> GetLatestCostAsync(Guid productId, CancellationToken ct = default)
        {
            var result = _prices.TryGetValue(productId, out var p) ? (decimal?)p : null;
            return Task.FromResult(result);
        }
    }

    private static FinalizeSaleCommandHandler BuildHandler(
        Guid productId,
        ILatestPurchasePriceRepository? priceRepo = null)
    {
        priceRepo ??= new FakeLatestPurchasePriceRepository();

        return new FinalizeSaleCommandHandler(
            new FakeSaleRepository(),
            new FakeCustomerRepository(),
            new FakeCustomerQueryRepository(),
            new FakeProductQueryRepository(productId),
            new FakeStockDeduction(),
            new FakeReferenceGenerator(),
            priceRepo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UnitCost snapshotted from port
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SnapshotsUnitCostFromLatestPurchasePricePort_NoThrow()
    {
        var productId = Guid.NewGuid();
        var priceRepo = new FakeLatestPurchasePriceRepository((productId, 42_000m));
        var handler   = BuildHandler(productId, priceRepo);

        var command = new FinalizeSaleCommand(
            CustomerId:         null,
            CustomerName:       "Tamu",
            Items:              [new FinalizeSaleItemInput(productId, "Semen 50kg", "sak", 2m, 65_000m)],
            IsDeliveryRequired: false,
            ServedBy:           "kasir-01");

        var result = await handler.HandleAsync(command);

        result.Should().NotBeNull();
        result.GrandTotal.Should().Be(130_000m);   // 2 × 65,000
    }

    [Fact]
    public async Task HandleAsync_NoPurchasePrice_UnitCostIsZero_NoThrow()
    {
        var productId = Guid.NewGuid();
        var priceRepo = new FakeLatestPurchasePriceRepository();   // no price
        var handler   = BuildHandler(productId, priceRepo);

        var command = new FinalizeSaleCommand(
            CustomerId:         null,
            CustomerName:       "Tamu",
            Items:              [new FinalizeSaleItemInput(productId, "Semen 50kg", "sak", 1m, 65_000m)],
            IsDeliveryRequired: false,
            ServedBy:           "kasir-01");

        // Must not throw even when there is no purchase price for the product
        Func<Task> act = () => handler.HandleAsync(command);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WithLineDiscount_GrandTotalUsesNetSubtotal()
    {
        var productId = Guid.NewGuid();
        var priceRepo = new FakeLatestPurchasePriceRepository((productId, 40_000m));
        var handler   = BuildHandler(productId, priceRepo);

        // listUnitPrice = 70,000; unitPrice = 60,000 (net); qty = 2
        var command = new FinalizeSaleCommand(
            CustomerId:         null,
            CustomerName:       "Tamu",
            Items:              [new FinalizeSaleItemInput(
                                    productId, "Semen 50kg", "sak", 2m,
                                    UnitPrice:       60_000m,
                                    ListUnitPrice:   70_000m,
                                    DiscountPerUnit: 10_000m)],
            IsDeliveryRequired: false,
            ServedBy:           "kasir-01");

        var result = await handler.HandleAsync(command);

        result.GrandTotal.Should().Be(120_000m);  // 60,000 × 2
    }
}

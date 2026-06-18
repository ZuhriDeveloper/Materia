using Materia.Application.Contracts.Common;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.Contracts.Sales;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.ChangeProductBaseUnit;

public class ChangeProductBaseUnitCommandHandler(
    IProductRepository productRepository,
    IStockRepository stockRepository,
    ISaleProductQueryService saleProductQueryService,
    IPurchaseOrderProductQueryService purchaseOrderProductQueryService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(ChangeProductBaseUnitCommand command, CancellationToken ct = default)
    {
        var productId = ProductId.From(command.ProductId);

        var product = await productRepository.GetByIdAsync(productId, ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        // Cross-aggregate pristine checks (most informative first):
        // 1. Sales reference (read model query — no event replay)
        if (await saleProductQueryService.ExistsForProductAsync(command.ProductId, ct))
            throw new DomainException("Cannot change the base unit because the product is referenced by a sale.");

        // 2. Purchase order reference (read model query — no event replay)
        if (await purchaseOrderProductQueryService.ExistsForProductAsync(command.ProductId, ct))
            throw new DomainException("Cannot change the base unit because the product is referenced by a purchase order.");

        // 3. Stock movements (enforced by Stock.CorrectUnit via domain guard).
        // Load the product-level stock (no variant). A product may legitimately have no
        // stock record yet — that means no movements and nothing to keep in sync.
        var stock = await stockRepository.GetAsync(productId, variantId: null, ct);

        // 4. In-aggregate guards (inactive / conversions / variants / same unit).
        product.ChangeBaseUnit(new UnitName(command.BaseUnit), command.ChangedBy);

        // 5. Stock aggregate guard (movements), and persist the unit correction when stock exists.
        stock?.CorrectUnit(command.BaseUnit, command.ChangedBy);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await productRepository.SaveAsync(product, ct);
            if (stock is not null)
                await stockRepository.SaveAsync(stock, ct);
        }, ct);
    }
}

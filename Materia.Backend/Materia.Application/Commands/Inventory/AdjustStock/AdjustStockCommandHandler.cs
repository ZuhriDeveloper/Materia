using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.AdjustStock;

public class AdjustStockCommandHandler(IStockRepository repository, IProductRepository productRepository)
{
    public async Task HandleAsync(AdjustStockCommand command, CancellationToken ct = default)
    {
        var productId = ProductId.From(command.ProductId);
        var variantId = command.VariantId is { } v ? VariantId.From(v) : null;
        var stock = await repository.GetAsync(productId, variantId, ct);

        if (stock is null)
        {
            // Auto-initialize stock if the product exists but has no stock record yet
            var product = await productRepository.GetByIdAsync(productId, ct)
                ?? throw new DomainException($"Product '{command.ProductId}' not found.");

            stock = Stock.Initialize(productId, product.BaseUnit.Value, command.AdjustedBy, variantId);
            await repository.SaveAsync(stock, ct);

            // Re-load so subsequent save appends correctly
            stock = await repository.GetAsync(productId, variantId, ct)!;
        }

        stock!.Adjust(command.Delta, command.Reason, command.AdjustedBy);
        await repository.SaveAsync(stock, ct);
    }
}

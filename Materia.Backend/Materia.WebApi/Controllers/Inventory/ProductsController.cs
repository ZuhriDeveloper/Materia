using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Inventory.AddColorVariant;
using Materia.Application.Commands.Inventory.AddUnitConversion;
using Materia.Application.Commands.Inventory.AdjustStock;
using Materia.Application.Commands.Inventory.AssignCategory;
using Materia.Application.Commands.Inventory.CreateProduct;
using Materia.Application.Commands.Inventory.RemoveCategory;
using Materia.Application.Commands.Inventory.RemoveColorVariant;
using Materia.Application.Commands.Inventory.RemoveUnitConversion;
using Materia.Application.Commands.Inventory.SetColorVariantStatus;
using Materia.Application.Commands.Inventory.SetProductStatus;
using Materia.Application.Commands.Inventory.SyncProductCategories;
using Materia.Application.Commands.Inventory.UpdateColorVariant;
using Materia.Application.Commands.Inventory.UpdateProduct;
using Materia.Application.Queries.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Inventory;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class ProductsController(
    CreateProductCommandHandler createHandler,
    UpdateProductCommandHandler updateHandler,
    SetProductStatusCommandHandler statusHandler,
    AssignCategoryToProductCommandHandler assignCategoryHandler,
    RemoveCategoryFromProductCommandHandler removeCategoryHandler,
    SyncProductCategoriesCommandHandler syncCategoriesHandler,
    AddUnitConversionCommandHandler addConversionHandler,
    RemoveUnitConversionCommandHandler removeConversionHandler,
    AddColorVariantCommandHandler addColorVariantHandler,
    UpdateColorVariantCommandHandler updateColorVariantHandler,
    RemoveColorVariantCommandHandler removeColorVariantHandler,
    SetColorVariantStatusCommandHandler setColorVariantStatusHandler,
    AdjustStockCommandHandler adjustStockHandler,
    GetProductByIdQueryHandler getByIdHandler,
    GetProductsQueryHandler getPagedHandler,
    GetStockByProductIdQueryHandler getStockHandler,
    GetProductStocksQueryHandler getProductStocksHandler,
    IValidator<CreateProductCommand> createValidator,
    IValidator<UpdateProductCommand> updateValidator,
    IValidator<AddUnitConversionCommand> addConversionValidator,
    IValidator<AddColorVariantCommand> addColorVariantValidator,
    IValidator<UpdateColorVariantCommand> updateColorVariantValidator,
    IValidator<AdjustStockCommand> adjustStockValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await getPagedHandler.HandleAsync(new GetProductsQuery(page, pageSize, isActive), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await getByIdHandler.HandleAsync(new GetProductByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var command = new CreateProductCommand(
            request.Name, request.Description, request.BaseUnit, request.CategoryIds ?? [], CurrentUser,
            request.SalePrice, request.Barcode);
        var validation = await createValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var command = new UpdateProductCommand(
            id, request.Name, request.Description, CurrentUser,
            request.SalePrice, request.Barcode);
        var validation = await updateValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await updateHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        await statusHandler.HandleAsync(new SetProductStatusCommand(id, request.IsActive, CurrentUser), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/categories/{categoryId:guid}")]
    public async Task<IActionResult> AssignCategory(Guid id, Guid categoryId, CancellationToken ct)
    {
        await assignCategoryHandler.HandleAsync(new AssignCategoryToProductCommand(id, categoryId, CurrentUser), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/categories/{categoryId:guid}")]
    public async Task<IActionResult> RemoveCategory(Guid id, Guid categoryId, CancellationToken ct)
    {
        await removeCategoryHandler.HandleAsync(new RemoveCategoryFromProductCommand(id, categoryId, CurrentUser), ct);
        return NoContent();
    }

    /// <summary>Replace the product's full category set in one call.</summary>
    [HttpPut("{id:guid}/categories")]
    public async Task<IActionResult> SyncCategories(
        Guid id, [FromBody] SyncCategoriesRequest request, CancellationToken ct)
    {
        await syncCategoriesHandler.HandleAsync(
            new SyncProductCategoriesCommand(id, request.CategoryIds, CurrentUser), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unit-conversions")]
    public async Task<IActionResult> AddUnitConversion(Guid id, [FromBody] AddUnitConversionRequest request, CancellationToken ct)
    {
        var command = new AddUnitConversionCommand(id, request.ToUnit, request.Factor, CurrentUser, request.SalePrice);
        var validation = await addConversionValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await addConversionHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/unit-conversions/{toUnit}")]
    public async Task<IActionResult> RemoveUnitConversion(Guid id, string toUnit, CancellationToken ct)
    {
        await removeConversionHandler.HandleAsync(new RemoveUnitConversionCommand(id, toUnit, CurrentUser), ct);
        return NoContent();
    }

    // ── Color Variants ──────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/color-variants")]
    public async Task<IActionResult> AddColorVariant(
        Guid id, [FromBody] AddColorVariantRequest request, CancellationToken ct)
    {
        var command = new AddColorVariantCommand(
            id, request.ColorName, request.ColorCode, request.Barcode, request.PriceOverride, CurrentUser);
        var validation = await addColorVariantValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var variantId = await addColorVariantHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id, variantId });
    }

    [HttpPut("{id:guid}/color-variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateColorVariant(
        Guid id, Guid variantId, [FromBody] UpdateColorVariantRequest request, CancellationToken ct)
    {
        var command = new UpdateColorVariantCommand(
            id, variantId, request.ColorName, request.ColorCode, request.Barcode, request.PriceOverride, CurrentUser);
        var validation = await updateColorVariantValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await updateColorVariantHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/color-variants/{variantId:guid}")]
    public async Task<IActionResult> RemoveColorVariant(Guid id, Guid variantId, CancellationToken ct)
    {
        await removeColorVariantHandler.HandleAsync(new RemoveColorVariantCommand(id, variantId, CurrentUser), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/color-variants/{variantId:guid}/status")]
    public async Task<IActionResult> SetColorVariantStatus(
        Guid id, Guid variantId, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        await setColorVariantStatusHandler.HandleAsync(
            new SetColorVariantStatusCommand(id, variantId, request.IsActive, CurrentUser), ct);
        return NoContent();
    }

    // ── Stock ─────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/stock")]
    public async Task<IActionResult> GetStock(Guid id, CancellationToken ct)
    {
        var result = await getStockHandler.HandleAsync(new GetStockByProductIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>All stock rows for the product, including one per color variant.</summary>
    [HttpGet("{id:guid}/variant-stock")]
    public async Task<IActionResult> GetVariantStock(Guid id, CancellationToken ct)
    {
        var result = await getProductStocksHandler.HandleAsync(new GetProductStocksQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/stock/adjust")]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        var command = new AdjustStockCommand(id, request.Delta, request.Reason, CurrentUser, request.VariantId);
        var validation = await adjustStockValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await adjustStockHandler.HandleAsync(command, ct);
        return NoContent();
    }
}

public record CreateProductRequest(
    string Name, string? Description, string BaseUnit, List<Guid>? CategoryIds,
    decimal SalePrice = 0m, string? Barcode = null);
public record UpdateProductRequest(
    string Name, string? Description,
    decimal SalePrice = 0m, string? Barcode = null);
public record SetStatusRequest(bool IsActive);
public record AddUnitConversionRequest(string ToUnit, decimal Factor, decimal SalePrice = 0m);
public record AddColorVariantRequest(
    string ColorName, string? ColorCode = null, string? Barcode = null, decimal? PriceOverride = null);
public record UpdateColorVariantRequest(
    string ColorName, string? ColorCode = null, string? Barcode = null, decimal? PriceOverride = null);
public record SyncCategoriesRequest(List<Guid> CategoryIds);
public record AdjustStockRequest(decimal Delta, string? Reason, Guid? VariantId = null);

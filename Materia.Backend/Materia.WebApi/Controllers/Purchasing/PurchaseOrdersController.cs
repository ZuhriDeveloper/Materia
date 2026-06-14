using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Purchasing.CancelPurchaseOrder;
using Materia.Application.Commands.Purchasing.ClosePurchaseOrder;
using Materia.Application.Commands.Purchasing.ConfirmPurchaseOrder;
using Materia.Application.Commands.Purchasing.CreatePurchaseOrder;
using Materia.Application.Commands.Purchasing.ReceivePurchaseOrder;
using Materia.Application.Commands.Purchasing.RecordPurchaseReturn;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.Queries.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Purchasing;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/purchase-orders")]
public class PurchaseOrdersController(
    CreatePurchaseOrderCommandHandler        createHandler,
    ConfirmPurchaseOrderCommandHandler       confirmHandler,
    ReceivePurchaseOrderCommandHandler       receiveHandler,
    RecordPurchaseReturnCommandHandler       returnHandler,
    ClosePurchaseOrderCommandHandler         closeHandler,
    CancelPurchaseOrderCommandHandler        cancelHandler,
    GetPurchaseOrdersQueryHandler            getAllHandler,
    GetPurchaseOrderByIdQueryHandler         getByIdHandler,
    ScanPurchaseInvoiceQueryHandler          scanHandler,
    IPurchaseInvoiceImageRepository          invoiceImageRepository,
    IValidator<CreatePurchaseOrderCommand>   createValidator,
    IValidator<ReceivePurchaseOrderCommand>  receiveValidator,
    IValidator<RecordPurchaseReturnCommand>  returnValidator,
    IValidator<ClosePurchaseOrderCommand>    closeValidator,
    IValidator<CancelPurchaseOrderCommand>   cancelValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var result = await getAllHandler.HandleAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await getByIdHandler.HandleAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── OCR / Invoice Scan ────────────────────────────────────────────────────

    [HttpPost("scan-invoice")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ScanInvoice(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var mediaType = ResolveMediaType(file);
        if (mediaType is null)
            return BadRequest(new { message = "Unsupported file type. Please upload a JPG, PNG, or PDF." });

        var bytes = await ReadAllBytesAsync(file, ct);
        var result = await scanHandler.HandleAsync(bytes, mediaType, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/invoice-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadInvoiceImage(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var mediaType = ResolveMediaType(file);
        if (mediaType is null)
            return BadRequest(new { message = "Unsupported file type. Please upload a JPG, PNG, or PDF." });

        var bytes = await ReadAllBytesAsync(file, ct);
        await invoiceImageRepository.SaveAsync(id, bytes, mediaType, file.FileName, CurrentUser, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/invoice-image")]
    public async Task<IActionResult> GetInvoiceImage(Guid id, CancellationToken ct)
    {
        var image = await invoiceImageRepository.GetByPurchaseOrderIdAsync(id, ct);
        return image is null
            ? NotFound()
            : File(image.Content, image.ContentType, image.FileName);
    }

    // Resolve by content-type first, then fall back to the file extension
    // (browsers/HttpClient sometimes send application/octet-stream).
    private static string? ResolveMediaType(IFormFile file) =>
        file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg"      => "image/jpeg",
            "image/jpg"       => "image/jpeg",
            "image/png"       => "image/png",
            "application/pdf" => "application/pdf",
            _ => Path.GetExtension(file.FileName).ToLowerInvariant() switch
            {
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".pdf"  => "application/pdf",
                _ => null,
            },
        };

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var command = new CreatePurchaseOrderCommand(
            request.SupplierId, request.Lines, CurrentUser,
            request.PaymentTermValue, request.PaymentTermUnit, request.UpdateCatalogOnReceipt);

        var validation = await createValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await confirmHandler.HandleAsync(new ConfirmPurchaseOrderCommand(id, CurrentUser), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(
        Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken ct)
    {
        var command = new ReceivePurchaseOrderCommand(id, request.Lines, CurrentUser);

        var validation = await receiveValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await receiveHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/return")]
    public async Task<IActionResult> Return(
        Guid id, [FromBody] RecordPurchaseReturnRequest request, CancellationToken ct)
    {
        var command = new RecordPurchaseReturnCommand(id, request.Lines, request.Reason, CurrentUser);

        var validation = await returnValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await returnHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(
        Guid id, [FromBody] ClosePurchaseOrderRequest request, CancellationToken ct)
    {
        var command = new ClosePurchaseOrderCommand(id, request.Reason, CurrentUser);

        var validation = await closeValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await closeHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelPurchaseOrderRequest request, CancellationToken ct)
    {
        var command = new CancelPurchaseOrderCommand(id, request.Reason, CurrentUser);

        var validation = await cancelValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await cancelHandler.HandleAsync(command, ct);
        return NoContent();
    }
}

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines,
    int? PaymentTermValue = null,
    string? PaymentTermUnit = null,
    bool UpdateCatalogOnReceipt = false);

public record ReceivePurchaseOrderRequest(
    IReadOnlyList<ReceivePurchaseOrderLineInput> Lines);

public record RecordPurchaseReturnRequest(
    IReadOnlyList<ReturnPurchaseOrderLineInput> Lines,
    string Reason);

public record ClosePurchaseOrderRequest(string Reason);

public record CancelPurchaseOrderRequest(string Reason);

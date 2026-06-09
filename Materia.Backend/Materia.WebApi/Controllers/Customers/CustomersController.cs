using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Customers.AddCustomerAddress;
using Materia.Application.Commands.Customers.CreateCustomer;
using Materia.Application.Commands.Customers.RemoveCustomerAddress;
using Materia.Application.Commands.Customers.SetCustomerStatus;
using Materia.Application.Commands.Customers.SetDefaultAddress;
using Materia.Application.Commands.Customers.UpdateCustomer;
using Materia.Application.Commands.Customers.UpdateCustomerAddress;
using Materia.Application.Queries.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Customers;

[ApiController]
[Authorize(Roles = "Admin,Cashier")]
[Route("api/[controller]")]
public class CustomersController(
    CreateCustomerCommandHandler           createHandler,
    UpdateCustomerCommandHandler           updateHandler,
    SetCustomerStatusCommandHandler        statusHandler,
    AddCustomerAddressCommandHandler       addAddressHandler,
    UpdateCustomerAddressCommandHandler    updateAddressHandler,
    RemoveCustomerAddressCommandHandler    removeAddressHandler,
    SetDefaultAddressCommandHandler        setDefaultHandler,
    GetCustomersQueryHandler               getHandler,
    GetNearbyCustomersQueryHandler         getNearbyHandler,
    IValidator<CreateCustomerCommand>      createValidator,
    IValidator<UpdateCustomerCommand>      updateValidator,
    IValidator<AddCustomerAddressCommand>  addAddressValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] bool?   isActive = null,
        CancellationToken ct = default)
    {
        var result = await getHandler.HandleAsync(
            new GetCustomersQuery(page, pageSize, search, isActive), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await getHandler.HandleByIdAsync(new GetCustomerByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] double  radiusKm   = 20,
        [FromQuery] int     maxResults = 50,
        CancellationToken ct = default)
    {
        var result = await getNearbyHandler.HandleAsync(
            new GetNearbyCustomersQuery(latitude, longitude, radiusKm, maxResults), ct);
        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var command    = new CreateCustomerCommand(request.Name, request.Phone, request.Email, CurrentUser);
        var validation = await createValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var command    = new UpdateCustomerCommand(id, request.Name, request.Phone, request.Email, CurrentUser);
        var validation = await updateValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await updateHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        await statusHandler.HandleAsync(new SetCustomerStatusCommand(id, request.IsActive, CurrentUser), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> AddAddress(
        Guid id, [FromBody] AddressRequest request, CancellationToken ct)
    {
        var command = new AddCustomerAddressCommand(
            id, request.Label, request.Street, request.City,
            request.Province, request.PostalCode,
            request.Latitude, request.Longitude, CurrentUser,
            request.Subdistrict, request.District);

        var validation = await addAddressValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var addressId = await addAddressHandler.HandleAsync(command, ct);
        return Ok(new { addressId });
    }

    [HttpPut("{id:guid}/addresses/{addressId:guid}")]
    public async Task<IActionResult> UpdateAddress(
        Guid id, Guid addressId, [FromBody] AddressRequest request, CancellationToken ct)
    {
        await updateAddressHandler.HandleAsync(new UpdateCustomerAddressCommand(
            id, addressId, request.Label, request.Street, request.City,
            request.Province, request.PostalCode,
            request.Latitude, request.Longitude, CurrentUser,
            request.Subdistrict, request.District), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/addresses/{addressId:guid}")]
    public async Task<IActionResult> RemoveAddress(
        Guid id, Guid addressId, CancellationToken ct)
    {
        await removeAddressHandler.HandleAsync(
            new RemoveCustomerAddressCommand(id, addressId, CurrentUser), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/addresses/{addressId:guid}/default")]
    public async Task<IActionResult> SetDefaultAddress(
        Guid id, Guid addressId, CancellationToken ct)
    {
        await setDefaultHandler.HandleAsync(
            new SetDefaultAddressCommand(id, addressId, CurrentUser), ct);
        return NoContent();
    }
}

public record CreateCustomerRequest(string Name, string Phone, string? Email);
public record UpdateCustomerRequest(string Name, string Phone, string? Email);
public record AddressRequest(
    string   Label,
    string   Street,
    string   City,
    string   Province,
    string?  PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string?  Subdistrict = null,   // Kelurahan / Desa
    string?  District    = null);  // Kecamatan
public record SetStatusRequest(bool IsActive);

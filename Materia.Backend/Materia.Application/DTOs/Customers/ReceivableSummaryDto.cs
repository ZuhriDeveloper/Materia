namespace Materia.Application.DTOs.Customers;

public record ReceivableSummaryDto(
    Guid    CustomerId,
    string  Name,
    string  Phone,
    decimal OutstandingDebt);

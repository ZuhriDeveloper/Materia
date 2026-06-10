namespace Materia.Application.Commands.Stores.UpdateStoreParameters;

public record UpdateStoreParametersCommand(
    string Address,
    string Phone,
    decimal MaxDeliveryDistanceKm,
    string UpdatedBy);

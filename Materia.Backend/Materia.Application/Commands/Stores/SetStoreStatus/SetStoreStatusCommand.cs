namespace Materia.Application.Commands.Stores.SetStoreStatus;

public record SetStoreStatusCommand(Guid StoreId, bool IsActive, string UpdatedBy);

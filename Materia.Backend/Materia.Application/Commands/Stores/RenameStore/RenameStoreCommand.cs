namespace Materia.Application.Commands.Stores.RenameStore;

public record RenameStoreCommand(Guid StoreId, string Name, string RenamedBy);

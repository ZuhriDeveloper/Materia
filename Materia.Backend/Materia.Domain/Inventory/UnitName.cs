using Materia.Domain.Common;

namespace Materia.Domain.Inventory;

public record UnitName
{
    public string Value { get; }

    public UnitName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Unit name cannot be empty.");
        Value = value.Trim();
    }

    public override string ToString() => Value;
}

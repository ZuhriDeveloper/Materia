using Materia.Domain.Common;

namespace Materia.Domain.Inventory;

/// <summary>
/// A color of a product variation. <see cref="Name"/> is the human label (e.g. "Merah").
/// <see cref="Code"/> is an optional manufacturer/tint identifier (e.g. "RAL 5010" or a hex
/// value) and is distinct from a scan barcode, which lives on <see cref="ColorVariant"/>.
/// </summary>
public record Color
{
    public string Name { get; }
    public string? Code { get; }

    public Color(string name, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Color name cannot be empty.");

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
    }

    public override string ToString() => Code is null ? Name : $"{Name} ({Code})";
}

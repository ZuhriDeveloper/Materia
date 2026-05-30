using System.Text.RegularExpressions;
using Materia.Domain.Common;

namespace Materia.Domain.Customers;

public record PhoneNumber
{
    private static readonly Regex Valid =
        new(@"^\+?[0-9\s\-\(\)]{7,20}$", RegexOptions.Compiled);

    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Nomor telepon tidak boleh kosong.");

        var normalized = value.Trim();
        if (!Valid.IsMatch(normalized))
            throw new DomainException($"Format nomor telepon '{normalized}' tidak valid.");

        Value = normalized;
    }

    public override string ToString() => Value;
}

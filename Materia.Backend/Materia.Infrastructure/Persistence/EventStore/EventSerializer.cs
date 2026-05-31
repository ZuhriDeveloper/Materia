using System.Text.Json;
using System.Text.Json.Serialization;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Inventory;
using Materia.Domain.Sales;

namespace Materia.Infrastructure.Persistence.EventStore;

public static class EventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new ProductIdConverter(),
            new CategoryIdConverter(),
            new UnitIdConverter(),
            new StockIdConverter(),
            new CustomerIdConverter(),
            new AddressIdConverter(),
            new SaleIdConverter(),
            new SaleItemIdConverter(),
        },
    };

    public static string Serialize(IDomainEvent evt) =>
        JsonSerializer.Serialize(evt, evt.GetType(), Options);

    public static IDomainEvent Deserialize(string eventType, string data)
    {
        var type = EventTypeRegistry.Resolve(eventType);
        return (IDomainEvent)JsonSerializer.Deserialize(data, type, Options)!;
    }

    // ── Custom converters for strongly-typed IDs ──────────────────────────────

    private sealed class ProductIdConverter : JsonConverter<ProductId>
    {
        public override ProductId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => ProductId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, ProductId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class CategoryIdConverter : JsonConverter<CategoryId>
    {
        public override CategoryId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => CategoryId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, CategoryId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class UnitIdConverter : JsonConverter<UnitId>
    {
        public override UnitId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => UnitId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, UnitId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class StockIdConverter : JsonConverter<StockId>
    {
        public override StockId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => StockId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, StockId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class CustomerIdConverter : JsonConverter<CustomerId>
    {
        public override CustomerId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => CustomerId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, CustomerId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class AddressIdConverter : JsonConverter<AddressId>
    {
        public override AddressId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => AddressId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, AddressId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class SaleIdConverter : JsonConverter<SaleId>
    {
        public override SaleId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => SaleId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, SaleId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }

    private sealed class SaleItemIdConverter : JsonConverter<SaleItemId>
    {
        public override SaleItemId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => SaleItemId.From(reader.GetGuid());
        public override void Write(Utf8JsonWriter writer, SaleItemId value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.Value);
    }
}

using Materia.Domain.Common;
using Materia.Domain.Customers.Events;
using Materia.Domain.Inventory.Events;

namespace Materia.Infrastructure.Persistence.EventStore;

public static class EventTypeRegistry
{
    private static readonly Dictionary<string, Type> Map = new()
    {
        // Product events
        [nameof(ProductCreated)]             = typeof(ProductCreated),
        [nameof(ProductNameUpdated)]         = typeof(ProductNameUpdated),
        [nameof(ProductDescriptionUpdated)]  = typeof(ProductDescriptionUpdated),
        [nameof(ProductDeactivated)]         = typeof(ProductDeactivated),
        [nameof(ProductActivated)]           = typeof(ProductActivated),
        [nameof(ProductCategoryAssigned)]    = typeof(ProductCategoryAssigned),
        [nameof(ProductCategoryRemoved)]     = typeof(ProductCategoryRemoved),
        [nameof(ProductUnitConversionAdded)] = typeof(ProductUnitConversionAdded),
        [nameof(ProductUnitConversionRemoved)] = typeof(ProductUnitConversionRemoved),

        // Category events
        [nameof(CategoryCreated)]            = typeof(CategoryCreated),
        [nameof(CategoryNameUpdated)]        = typeof(CategoryNameUpdated),
        [nameof(CategoryDescriptionUpdated)] = typeof(CategoryDescriptionUpdated),
        [nameof(CategoryActivated)]          = typeof(CategoryActivated),
        [nameof(CategoryDeactivated)]        = typeof(CategoryDeactivated),

        // Unit events
        [nameof(UnitCreated)]     = typeof(UnitCreated),
        [nameof(UnitUpdated)]     = typeof(UnitUpdated),
        [nameof(UnitActivated)]   = typeof(UnitActivated),
        [nameof(UnitDeactivated)] = typeof(UnitDeactivated),

        // Stock events
        [nameof(StockInitialized)] = typeof(StockInitialized),
        [nameof(StockAdjusted)]    = typeof(StockAdjusted),

        // Customer events
        [nameof(CustomerCreated)]               = typeof(CustomerCreated),
        [nameof(CustomerUpdated)]               = typeof(CustomerUpdated),
        [nameof(CustomerActivated)]             = typeof(CustomerActivated),
        [nameof(CustomerDeactivated)]           = typeof(CustomerDeactivated),
        [nameof(CustomerAddressAdded)]          = typeof(CustomerAddressAdded),
        [nameof(CustomerAddressUpdated)]        = typeof(CustomerAddressUpdated),
        [nameof(CustomerAddressRemoved)]        = typeof(CustomerAddressRemoved),
        [nameof(CustomerDefaultAddressChanged)] = typeof(CustomerDefaultAddressChanged),
    };

    public static Type Resolve(string eventType) =>
        Map.TryGetValue(eventType, out var type)
            ? type
            : throw new InvalidOperationException($"Unknown event type: '{eventType}'.");

    public static string GetName(IDomainEvent evt) => evt.GetType().Name;
}

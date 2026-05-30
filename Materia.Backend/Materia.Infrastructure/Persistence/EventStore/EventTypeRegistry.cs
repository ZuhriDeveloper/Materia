using Materia.Domain.Common;
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
    };

    public static Type Resolve(string eventType) =>
        Map.TryGetValue(eventType, out var type)
            ? type
            : throw new InvalidOperationException($"Unknown event type: '{eventType}'.");

    public static string GetName(IDomainEvent evt) => evt.GetType().Name;
}

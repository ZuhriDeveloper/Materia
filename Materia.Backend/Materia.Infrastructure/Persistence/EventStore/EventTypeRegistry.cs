using Materia.Domain.Common;
using Materia.Domain.Customers.Events;
using Materia.Domain.Financials.Events;
using Materia.Domain.Inventory.Events;
using Materia.Domain.Purchasing.Events;
using Materia.Domain.Sales.Events;
using Materia.Domain.Stores.Events;

namespace Materia.Infrastructure.Persistence.EventStore;

public static class EventTypeRegistry
{
    private static readonly Dictionary<string, Type> Map = new()
    {
        // Product events
        [nameof(ProductCreated)]             = typeof(ProductCreated),
        [nameof(ProductNameUpdated)]         = typeof(ProductNameUpdated),
        [nameof(ProductSalePriceChanged)]    = typeof(ProductSalePriceChanged),
        [nameof(ProductBarcodeChanged)]      = typeof(ProductBarcodeChanged),
        [nameof(ProductDescriptionUpdated)]  = typeof(ProductDescriptionUpdated),
        [nameof(ProductDeactivated)]         = typeof(ProductDeactivated),
        [nameof(ProductActivated)]           = typeof(ProductActivated),
        [nameof(ProductCategoryAssigned)]    = typeof(ProductCategoryAssigned),
        [nameof(ProductCategoryRemoved)]     = typeof(ProductCategoryRemoved),
        [nameof(ProductUnitConversionAdded)] = typeof(ProductUnitConversionAdded),
        [nameof(ProductUnitConversionRemoved)] = typeof(ProductUnitConversionRemoved),
        [nameof(ProductColorVariantAdded)]     = typeof(ProductColorVariantAdded),
        [nameof(ProductColorVariantUpdated)]   = typeof(ProductColorVariantUpdated),
        [nameof(ProductColorVariantRemoved)]   = typeof(ProductColorVariantRemoved),
        [nameof(ProductColorVariantDeactivated)] = typeof(ProductColorVariantDeactivated),
        [nameof(ProductColorVariantActivated)] = typeof(ProductColorVariantActivated),

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
        [nameof(StockInitialized)]            = typeof(StockInitialized),
        [nameof(StockAdjusted)]               = typeof(StockAdjusted),
        [nameof(StockReconciledFromPurchase)] = typeof(StockReconciledFromPurchase),
        [nameof(StockReducedFromPurchaseReturn)] = typeof(StockReducedFromPurchaseReturn),

        // Customer events
        [nameof(CustomerCreated)]               = typeof(CustomerCreated),
        [nameof(CustomerUpdated)]               = typeof(CustomerUpdated),
        [nameof(CustomerActivated)]             = typeof(CustomerActivated),
        [nameof(CustomerDeactivated)]           = typeof(CustomerDeactivated),
        [nameof(CustomerAddressAdded)]          = typeof(CustomerAddressAdded),
        [nameof(CustomerAddressUpdated)]        = typeof(CustomerAddressUpdated),
        [nameof(CustomerAddressRemoved)]        = typeof(CustomerAddressRemoved),
        [nameof(CustomerDefaultAddressChanged)] = typeof(CustomerDefaultAddressChanged),
        [nameof(CustomerDebtIncurred)]          = typeof(CustomerDebtIncurred),
        [nameof(ReceivablePaymentRecorded)]     = typeof(ReceivablePaymentRecorded),

        // Supplier events
        [nameof(SupplierRegistered)]       = typeof(SupplierRegistered),
        [nameof(SupplierUpdated)]          = typeof(SupplierUpdated),
        [nameof(SupplierActivated)]        = typeof(SupplierActivated),
        [nameof(SupplierDeactivated)]      = typeof(SupplierDeactivated),
        [nameof(SupplierPurchasePriceSet)] = typeof(SupplierPurchasePriceSet),

        // Purchase order events
        [nameof(PurchaseOrderCreated)]   = typeof(PurchaseOrderCreated),
        [nameof(PurchaseOrderConfirmed)] = typeof(PurchaseOrderConfirmed),
        [nameof(PurchaseOrderReceived)]  = typeof(PurchaseOrderReceived),
        [nameof(PurchaseOrderReturned)]  = typeof(PurchaseOrderReturned),
        [nameof(PurchaseOrderCancelled)] = typeof(PurchaseOrderCancelled),

        // Sale events
        [nameof(SaleCreated)]            = typeof(SaleCreated),
        [nameof(SaleCustomerSet)]        = typeof(SaleCustomerSet),
        [nameof(SaleTypeSetToPickup)]    = typeof(SaleTypeSetToPickup),
        [nameof(SaleTypeSetToDelivery)]  = typeof(SaleTypeSetToDelivery),
        [nameof(SaleItemAdded)]          = typeof(SaleItemAdded),
        [nameof(SaleItemRemoved)]        = typeof(SaleItemRemoved),
        [nameof(SaleConfirmed)]          = typeof(SaleConfirmed),
        [nameof(SalePaid)]               = typeof(SalePaid),
        [nameof(SaleSettled)]            = typeof(SaleSettled),
        [nameof(SaleCancelled)]          = typeof(SaleCancelled),
        [nameof(SaleFinalized)]          = typeof(SaleFinalized),
        [nameof(DeliveryRequested)]      = typeof(DeliveryRequested),

        // Petty cash (financials) events
        [nameof(PettyCashExpenseRecorded)] = typeof(PettyCashExpenseRecorded),
        [nameof(ChangeFundDeposited)]      = typeof(ChangeFundDeposited),
        [nameof(ChangeFundWithdrawn)]      = typeof(ChangeFundWithdrawn),

        // Store (platform / multi-tenant) events
        [nameof(StoreRegistered)]         = typeof(StoreRegistered),
        [nameof(StoreRenamed)]            = typeof(StoreRenamed),
        [nameof(StoreActivated)]          = typeof(StoreActivated),
        [nameof(StoreDeactivated)]        = typeof(StoreDeactivated),
        [nameof(StoreParametersUpdated)]  = typeof(StoreParametersUpdated),
    };

    public static Type Resolve(string eventType) =>
        Map.TryGetValue(eventType, out var type)
            ? type
            : throw new InvalidOperationException($"Unknown event type: '{eventType}'.");

    public static string GetName(IDomainEvent evt) => evt.GetType().Name;
}

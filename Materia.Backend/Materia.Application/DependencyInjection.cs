using FluentValidation;
using Materia.Application.Commands.Auth;
using Materia.Application.Services;
using Materia.Application.Commands.Customers.AddCustomerAddress;
using Materia.Application.Commands.Customers.CreateCustomer;
using Materia.Application.Commands.Customers.RemoveCustomerAddress;
using Materia.Application.Commands.Customers.SetCustomerStatus;
using Materia.Application.Commands.Customers.SetDefaultAddress;
using Materia.Application.Commands.Customers.UpdateCustomer;
using Materia.Application.Commands.Customers.UpdateCustomerAddress;
using Materia.Application.Queries.Customers;
using Materia.Application.Commands.Inventory.AddColorVariant;
using Materia.Application.Commands.Inventory.AddUnitConversion;
using Materia.Application.Commands.Inventory.AdjustStock;
using Materia.Application.Commands.Inventory.AssignCategory;
using Materia.Application.Commands.Inventory.CreateCategory;
using Materia.Application.Commands.Inventory.CreateProduct;
using Materia.Application.Commands.Inventory.CreateUnit;
using Materia.Application.Commands.Inventory.SetCategoryStatus;
using Materia.Application.Commands.Inventory.SetUnitStatus;
using Materia.Application.Commands.Inventory.UpdateCategory;
using Materia.Application.Commands.Inventory.UpdateUnit;
using Materia.Application.Commands.Inventory.RemoveCategory;
using Materia.Application.Commands.Inventory.RemoveColorVariant;
using Materia.Application.Commands.Inventory.RemoveUnitConversion;
using Materia.Application.Commands.Inventory.SetColorVariantStatus;
using Materia.Application.Commands.Inventory.SetProductStatus;
using Materia.Application.Commands.Inventory.SyncProductCategories;
using Materia.Application.Commands.Inventory.UpdateColorVariant;
using Materia.Application.Commands.Inventory.UpdateProduct;
using Materia.Application.Commands.Purchasing.CancelPurchaseOrder;
using Materia.Application.Commands.Purchasing.ConfirmPurchaseOrder;
using Materia.Application.Commands.Purchasing.CreatePurchaseOrder;
using Materia.Application.Commands.Purchasing.ReceivePurchaseOrder;
using Materia.Application.Commands.Purchasing.RegisterSupplier;
using Materia.Application.Commands.Purchasing.SetPurchasePrice;
using Materia.Application.Commands.Purchasing.SetSupplierStatus;
using Materia.Application.Commands.Purchasing.UpdateSupplier;
using Materia.Application.Queries.Inventory;
using Materia.Application.Queries.Purchasing;
using Materia.Application.Commands.Customers.RecordReceivablePayment;
using Materia.Application.Commands.Sales.FinalizeSale;
using Materia.Application.Commands.Financials.RecordPettyCashExpense;
using Materia.Application.Commands.Financials.RecordChangeFundDeposit;
using Materia.Application.Commands.Financials.RecordChangeFundWithdrawal;
using Materia.Application.Financials.Queries;
using Materia.Application.Commands.Stores.RegisterStore;
using Materia.Application.Commands.Stores.RenameStore;
using Materia.Application.Commands.Stores.SetStoreStatus;
using Materia.Application.Commands.Stores.UpdateStoreParameters;
using Materia.Application.Queries.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Materia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<LoginCommandHandler>();

        // Inventory — product commands
        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<UpdateProductCommandHandler>();
        services.AddScoped<SetProductStatusCommandHandler>();
        services.AddScoped<AssignCategoryToProductCommandHandler>();
        services.AddScoped<RemoveCategoryFromProductCommandHandler>();
        services.AddScoped<AddUnitConversionCommandHandler>();
        services.AddScoped<RemoveUnitConversionCommandHandler>();
        services.AddScoped<SyncProductCategoriesCommandHandler>();
        services.AddScoped<AddColorVariantCommandHandler>();
        services.AddScoped<UpdateColorVariantCommandHandler>();
        services.AddScoped<RemoveColorVariantCommandHandler>();
        services.AddScoped<SetColorVariantStatusCommandHandler>();

        // Inventory — category commands
        services.AddScoped<CreateCategoryCommandHandler>();
        services.AddScoped<UpdateCategoryCommandHandler>();
        services.AddScoped<SetCategoryStatusCommandHandler>();

        // Inventory — unit commands
        services.AddScoped<CreateUnitCommandHandler>();
        services.AddScoped<UpdateUnitCommandHandler>();
        services.AddScoped<SetUnitStatusCommandHandler>();

        // Inventory — stock commands
        services.AddScoped<AdjustStockCommandHandler>();

        // Inventory — queries
        services.AddScoped<GetProductByIdQueryHandler>();
        services.AddScoped<GetProductsQueryHandler>();
        services.AddScoped<GetCategoriesQueryHandler>();
        services.AddScoped<GetUnitsQueryHandler>();
        services.AddScoped<GetStockByProductIdQueryHandler>();
        services.AddScoped<GetProductStocksQueryHandler>();

        // Customer commands
        services.AddScoped<CreateCustomerCommandHandler>();
        services.AddScoped<UpdateCustomerCommandHandler>();
        services.AddScoped<SetCustomerStatusCommandHandler>();
        services.AddScoped<AddCustomerAddressCommandHandler>();
        services.AddScoped<UpdateCustomerAddressCommandHandler>();
        services.AddScoped<RemoveCustomerAddressCommandHandler>();
        services.AddScoped<SetDefaultAddressCommandHandler>();

        // Customer queries
        services.AddScoped<GetCustomersQueryHandler>();
        services.AddScoped<GetNearbyCustomersQueryHandler>();
        services.AddScoped<GetOutstandingReceivablesQueryHandler>();

        // Customer AR (receivables)
        services.AddScoped<RecordReceivablePaymentCommandHandler>();

        // Purchasing — supplier commands
        services.AddScoped<RegisterSupplierCommandHandler>();
        services.AddScoped<UpdateSupplierCommandHandler>();
        services.AddScoped<SetSupplierStatusCommandHandler>();
        services.AddScoped<SetPurchasePriceCommandHandler>();

        // Purchasing — PO commands
        services.AddScoped<CreatePurchaseOrderCommandHandler>();
        services.AddScoped<ConfirmPurchaseOrderCommandHandler>();
        services.AddScoped<ReceivePurchaseOrderCommandHandler>();
        services.AddScoped<CancelPurchaseOrderCommandHandler>();

        // Purchasing — queries
        services.AddScoped<GetSuppliersQueryHandler>();
        services.AddScoped<GetSupplierByIdQueryHandler>();
        services.AddScoped<GetPurchaseOrdersQueryHandler>();
        services.AddScoped<GetPurchaseOrderByIdQueryHandler>();
        services.AddScoped<ScanPurchaseInvoiceQueryHandler>();

        // Sales
        services.AddScoped<SaleService>();
        services.AddScoped<FinalizeSaleCommandHandler>();

        // Financials
        services.AddScoped<GetProfitAndLossQueryHandler>();
        services.AddScoped<GetCashFlowQueryHandler>();
        services.AddScoped<RecordPettyCashExpenseCommandHandler>();
        services.AddScoped<GetPettyCashExpensesQueryHandler>();
        services.AddScoped<RecordChangeFundDepositCommandHandler>();
        services.AddScoped<RecordChangeFundWithdrawalCommandHandler>();
        services.AddScoped<GetChangeFundQueryHandler>();
        services.AddScoped<GetChangeFundWithdrawalsQueryHandler>();

        // Stores (platform / multi-tenant)
        services.AddScoped<RegisterStoreCommandHandler>();
        services.AddScoped<RenameStoreCommandHandler>();
        services.AddScoped<SetStoreStatusCommandHandler>();
        services.AddScoped<UpdateStoreParametersCommandHandler>();
        services.AddScoped<GetStoresQueryHandler>();
        services.AddScoped<GetMyStoreQueryHandler>();

        // Validators (all assemblies scanned from this project)
        services.AddValidatorsFromAssemblyContaining<LoginCommandHandler>();

        return services;
    }
}

using FluentValidation;
using Materia.Application.Commands.Auth;
using Materia.Application.Commands.Customers.AddCustomerAddress;
using Materia.Application.Commands.Customers.CreateCustomer;
using Materia.Application.Commands.Customers.RemoveCustomerAddress;
using Materia.Application.Commands.Customers.SetCustomerStatus;
using Materia.Application.Commands.Customers.SetDefaultAddress;
using Materia.Application.Commands.Customers.UpdateCustomer;
using Materia.Application.Commands.Customers.UpdateCustomerAddress;
using Materia.Application.Queries.Customers;
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
using Materia.Application.Commands.Inventory.RemoveUnitConversion;
using Materia.Application.Commands.Inventory.SetProductStatus;
using Materia.Application.Commands.Inventory.SyncProductCategories;
using Materia.Application.Commands.Inventory.UpdateProduct;
using Materia.Application.Queries.Inventory;
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

        // Validators (all assemblies scanned from this project)
        services.AddValidatorsFromAssemblyContaining<LoginCommandHandler>();

        return services;
    }
}

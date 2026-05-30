using FluentValidation;
using Materia.Application.Commands.Auth;
using Materia.Application.Commands.Inventory.AddUnitConversion;
using Materia.Application.Commands.Inventory.AssignCategory;
using Materia.Application.Commands.Inventory.CreateCategory;
using Materia.Application.Commands.Inventory.CreateProduct;
using Materia.Application.Commands.Inventory.SetCategoryStatus;
using Materia.Application.Commands.Inventory.UpdateCategory;
using Materia.Application.Commands.Inventory.RemoveCategory;
using Materia.Application.Commands.Inventory.RemoveUnitConversion;
using Materia.Application.Commands.Inventory.SetProductStatus;
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

        // Inventory — commands
        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<UpdateProductCommandHandler>();
        services.AddScoped<SetProductStatusCommandHandler>();
        services.AddScoped<AssignCategoryToProductCommandHandler>();
        services.AddScoped<RemoveCategoryFromProductCommandHandler>();
        services.AddScoped<AddUnitConversionCommandHandler>();
        services.AddScoped<RemoveUnitConversionCommandHandler>();
        services.AddScoped<CreateCategoryCommandHandler>();
        services.AddScoped<UpdateCategoryCommandHandler>();
        services.AddScoped<SetCategoryStatusCommandHandler>();

        // Inventory — queries
        services.AddScoped<GetProductByIdQueryHandler>();
        services.AddScoped<GetProductsQueryHandler>();
        services.AddScoped<GetCategoriesQueryHandler>();

        // Validators (all assemblies scanned from this project)
        services.AddValidatorsFromAssemblyContaining<LoginCommandHandler>();

        return services;
    }
}

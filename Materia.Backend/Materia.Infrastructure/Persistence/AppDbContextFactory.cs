using Materia.Application.Contracts.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Materia.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can construct the context without the full app
/// DI graph (notably <see cref="ICurrentStore"/> / IHttpContextAccessor). The connection
/// string is a placeholder — scaffolding a migration only needs the model, not a live DB.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=materiadb;Username=postgres;Password=postgres")
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new AppDbContext(options, new DesignTimeCurrentStore());
    }

    private sealed class DesignTimeCurrentStore : ICurrentStore
    {
        public Guid StoreId => throw new InvalidOperationException("No store at design time.");
        public Guid? StoreIdOrNull => null;
        public bool IsPlatformAdmin => true;
    }
}

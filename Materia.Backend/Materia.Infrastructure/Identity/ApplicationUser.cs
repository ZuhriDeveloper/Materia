using Microsoft.AspNetCore.Identity;

namespace Materia.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>
    /// The store this user belongs to. <c>null</c> for a platform <c>SuperAdmin</c>, who
    /// is not bound to a single store and instead manages stores and provisions users.
    /// </summary>
    public Guid? StoreId { get; set; }
}

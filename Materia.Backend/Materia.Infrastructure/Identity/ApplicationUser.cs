using Microsoft.AspNetCore.Identity;

namespace Materia.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}

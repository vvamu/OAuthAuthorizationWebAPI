using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace OAuthAuthorization.Domain.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    [Required]
    public override string UserName { get; set; }
    [Required]
    [NotNull]
    public string Login { get; set; }

}

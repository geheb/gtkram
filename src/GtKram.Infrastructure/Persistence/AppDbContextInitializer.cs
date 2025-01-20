namespace GtKram.Infrastructure.Persistence;

using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

internal sealed class AppDbContextInitializer
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly IConfiguration _configuration;
    private readonly UserManager<IdentityUserGuid> _userManager;

    public AppDbContextInitializer(
        IConfiguration configuration,
        UserManager<IdentityUserGuid> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task CreateSuperAdmin()
    {
        const string emailKey = "Bootstrap:SuperUser:Email";
        var superUserEmail = _configuration[emailKey];
        if (string.IsNullOrEmpty(superUserEmail))
        {
            throw new InvalidProgramException(emailKey);
        }

        var superUser = await _userManager.FindByEmailAsync(superUserEmail);

        if (superUser != null)
        {
            return;
        }

        var superUserName = _configuration["Bootstrap:SuperUser:Name"];

        superUser = new IdentityUserGuid
        {
            Id = _pkGenerator.Generate(),
            Name = superUserName,
            UserName = Guid.NewGuid().ToString().Replace("-", string.Empty),
            Email = superUserEmail,
            EmailConfirmed = true,
        };

        const string passKey = "Bootstrap:SuperUser:Password";
        var password = _configuration[passKey];
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidProgramException(passKey);
        }

        var result = await _userManager.CreateAsync(superUser, password);
        if (!result.Succeeded)
        {
            throw new InvalidProgramException("Add super user failed: " + result);
        }

        result = await _userManager.AddToRolesAsync(superUser, new[] { Roles.Admin });
        if (!result.Succeeded)
        {
            throw new InvalidProgramException("Add super user roles failed: " + result);
        }
    }
}

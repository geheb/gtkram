using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Web;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Users : IUsers
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly ILogger _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<IdentityUserGuid> _userManager;
    private readonly IHttpContextAccessor _httpContext;
    private readonly LinkGenerator _linkGenerator;
    private readonly IEmailValidatorService _emailValidator;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public Users(
        ILogger<Users> logger,
        AppDbContext dbContext,
        UserManager<IdentityUserGuid> userManager,
        IHttpContextAccessor httpContext,
        LinkGenerator linkGenerator,
        IEmailValidatorService emailValidator,
        IDataProtectionProvider dataProtectionProvider)
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContext = httpContext;
        _linkGenerator = linkGenerator;
        _emailValidator = emailValidator;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public async Task NotifyPasswordForgotten(string email, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("User {Email} not found", email);
            return;
        }

        await NotifyChangePassword(user, cancellationToken);
    }

    public async Task<string[]?> Update(UserDto dto, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(dto.Id.ToString());
        if (user == null)
        {
            return new[] { "Benutzer wurde nicht gefunden" };
        }

        if (!dto.Email!.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var isValid = await _emailValidator.Validate(dto.Email, cancellationToken);
            if (!isValid) return new[] { "Die E-Mail-Adresse ist ungültig" };

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, dto.Email);
            var result = await _userManager.ChangeEmailAsync(user, dto.Email, token);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => e.Description).ToArray();
            }
        }

        if (!string.IsNullOrEmpty(password))
        {
            foreach (var validator in _userManager.PasswordValidators)
            {
                var r = await validator.ValidateAsync(_userManager, user, password);
                if (!r.Succeeded)
                {
                    return r.Errors.Select(e => e.Description).ToArray();
                }
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => e.Description).ToArray();
            }

            if (!user.EmailConfirmed)
            {
                token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return result.Errors.Select(e => e.Description).ToArray();
                }
            }
        }

        if (!(dto.Name ?? string.Empty).Equals(user.Name, StringComparison.Ordinal))
        {
            user.Name = dto.Name;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => e.Description).ToArray();
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var removeRoles = roles.Except(dto.Roles!).ToArray();
        var addRoles = dto.Roles!.Except(roles).ToArray();

        if (removeRoles.Length > 0)
        {
            var result = await _userManager.RemoveFromRolesAsync(user, removeRoles);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => e.Description).ToArray();
            }
        }

        if (addRoles.Length > 0)
        {
            var result = await _userManager.AddToRolesAsync(user, addRoles);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => e.Description).ToArray();
            }
        }

        return null;
    }

    public async Task<string[]?> Update(Guid id, string name)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return new[] { "Benutzer wurde nicht gefunden" };
        }

        bool hasChanges = false;

        if (!(name ?? string.Empty).Equals(user.Name, StringComparison.Ordinal))
        {
            hasChanges = true;
            user.Name = name;
        }

        if (hasChanges)
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return result.Errors.Select(e => e.Description).ToArray();
            }
        }

        return null;
    }

    public async Task<string?> VerfiyChangePassword(Guid id, string token)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {Id} not found", id);
            return null;
        }

        token = HttpUtility.UrlDecode(token);

        var isTokenValid = await _userManager.VerifyUserTokenAsync(user,
            _userManager.Options.Tokens.PasswordResetTokenProvider,
            UserManager<IdentityUserGuid>.ResetPasswordTokenPurpose,
            token);


        if (!isTokenValid)
        {
            _logger.LogError("Verify token for user {Email} failed", user.Email);
            return null;
        }

        return user.Email;
    }

    public async Task<string?> VerifyConfirmRegistration(Guid id, string token)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            _logger.LogError("User {Id} not found", id);
            return null;
        }

        token = HttpUtility.UrlDecode(token);

        var isUserTokenValid = await _userManager.VerifyUserTokenAsync(user,
            _userManager.Options.Tokens.EmailConfirmationTokenProvider,
            UserManager<IdentityUserGuid>.ConfirmEmailTokenPurpose,
            token);

        if (!isUserTokenValid)
        {
            _logger.LogError("User {Email} has invalid token", user.Email);
            return null;
        }

        return user.Email;
    }

    public async Task<(string[]? Error, string? Email)> ChangePassword(Guid id, string? token, string password)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {Id} not found", id);
            return (null, null);
        }

        if (string.IsNullOrEmpty(token))
        {
            token = await _userManager.GeneratePasswordResetTokenAsync(user);
        }
        else
        {
            token = HttpUtility.UrlDecode(token);

            var isTokenValid = await _userManager.VerifyUserTokenAsync(user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<IdentityUserGuid>.ResetPasswordTokenPurpose,
                token);

            if (!isTokenValid)
            {
                _logger.LogError("Verify token for user {Email} failed", user.Email);
                return (null, null);
            }
        }

        foreach (var validator in _userManager.PasswordValidators)
        {
            var r = await validator.ValidateAsync(_userManager, user, password);
            if (!r.Succeeded)
            {
                return (r.Errors.Select(r => r.Description).ToArray(), user.Email);
            }
        }

        var result = await _userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return (result.Errors.Select(r => r.Description).ToArray(), user.Email);
        }

        return (null, user.Email);
    }

    public async Task<(string? Error, bool IsFatal)> NotifyConfirmChangeEmail(Guid id, string newEmail, string currentPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return ("Benutzer nicht gefunden", true);
        }

        if (await _userManager.FindByEmailAsync(newEmail) != null)
        {
            return ("Die neue E-Mail-Adresse ist bereits vergeben", false);
        }

        if (!await _emailValidator.Validate(newEmail, cancellationToken))
        {
            return ("Neue E-Mail-Adresse ist ungültig", false);
        }

        var result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, currentPassword);
        if (result != PasswordVerificationResult.Success)
        {
            return ("Das angegebene Passwort stimmt nicht überein", false);
        }

        if (!await NotifyConfirmChangeEmail(user, newEmail, cancellationToken))
        {
            return ("Fehler beim Speichern", true);
        }

        return (null, false);
    }

    public async Task<string[]?> ConfirmRegistrationAndSetPassword(Guid id, string token, string password)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            _logger.LogError("User {Id} not found", id);
            return new[] { "Benutzer wurde nicht gefunden" };
        }

        token = HttpUtility.UrlDecode(token);

        var isUserTokenValid = await _userManager.VerifyUserTokenAsync(user,
            _userManager.Options.Tokens.EmailConfirmationTokenProvider,
            UserManager<IdentityUserGuid>.ConfirmEmailTokenPurpose,
            token);

        if (!isUserTokenValid)
        {
            _logger.LogError("User {Email} has invalid token", user.Email);
            return new[] { "Der Link ist ungültig oder abgelaufen." };
        }

        var identityResult = await _userManager.ConfirmEmailAsync(user, token);
        if (!identityResult.Succeeded)
        {
            return identityResult.Errors.Select(r => r.Description).ToArray();
        }

        var result = await ChangePassword(id, null, password);
        return result.Error;
    }

    public async Task<bool> NotifyConfirmRegistration(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            _logger.LogError("User {Id} not found", id);
            return false;
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation("User {Email} is already confirmed", user.Email);
            return true;
        }

        return await NotifyConfirmRegistration(user, cancellationToken);
    }

    public async Task<UserDto[]> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .Include(e => e.UserRoles!)
            .ThenInclude(e => e.Role)
            .OrderBy(e => e.Name)
            .ToArrayAsync(cancellationToken);

        var idn = new IdnMapping();
        var dc = new GermanDateTimeConverter();

        return users.Select(e => e.MapToDto(idn, dc)).ToArray();
    }

    public async Task<UserDto?> Find(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .Include(e => e.UserRoles!)
            .ThenInclude(e => e.Role)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (user == null) return null;

        var idn = new IdnMapping();
        var dc = new GermanDateTimeConverter();

        return user.MapToDto(idn, dc);
    }

    public async Task<Guid?> CreateSeller(string email, string name, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            if (!user.EmailConfirmed)
            {
                if (!await NotifyConfirmRegistration(user, cancellationToken)) return default;
                return user.Id;
            }

            if (await _userManager.IsInRoleAsync(user, Roles.Seller))
            {
                return user.Id;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Seller);
            if (!roleResult.Succeeded)
            {
                var error = string.Join(";", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Add seller role for user {Email} failed: {Error}", email, error);
                return default;
            }

            return user.Id;
        }

        user = new IdentityUserGuid
        {
            UserName = Guid.NewGuid().ToString().Replace("-", string.Empty),
            Name = name,
            Email = email
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var error = string.Join(";", result.Errors.Select(e => e.Description));
            _logger.LogError("Create user {Email} failed: {Error}", email, error);
            return default;
        }

        result = await _userManager.AddToRoleAsync(user, Roles.Seller);
        if (!result.Succeeded)
        {
            var error = string.Join(";", result.Errors.Select(e => e.Description));
            _logger.LogError("Add role for user {Email} failed: {Error}", email, error);
            return default;
        }

        if (!await NotifyConfirmRegistration(user, cancellationToken)) return default;
        return user.Id;
    }

    public async Task<bool> AddBillingRole(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await _userManager.FindByIdAsync(userId.ToString());
        if (entity == null)
        {
            _logger.LogError("User with {Id} not found", userId);
            return false;
        }

        if (await _userManager.IsInRoleAsync(entity, Roles.Billing))
        {
            return true;
        }

        var result = await _userManager.AddToRoleAsync(entity, Roles.Billing);
        if (!result.Succeeded)
        {
            var error = string.Join(";", result.Errors.Select(e => e.Description));
            _logger.LogError("Add role for user {Email} failed: {Error}", entity.Email, error);
            return false;
        }
        return true;
    }

    public async Task<string[]?> Create(UserDto dto, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email!);
        if (user != null)
        {
            return new[] { "Der Benutzer mit der E-Mail-Adresse existiert bereits." };
        }

        if (!await _emailValidator.Validate(dto.Email!, cancellationToken))
        {
            return new[] { "Die E-Mail-Adresse ist ungültig." };
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        user = new IdentityUserGuid
        {
            UserName = Guid.NewGuid().ToString().Replace("-", string.Empty),
            Name = dto.Name,
            Email = dto.Email
        };
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return result.Errors.Select(e => e.Description).ToArray();
        }

        result = await _userManager.AddToRolesAsync(user, dto.Roles!);
        if (!result.Succeeded)
        {
            return result.Errors.Select(e => e.Description).ToArray();
        }

        if (!await NotifyConfirmRegistration(user, cancellationToken))
        {
            return new[] { "Fehler beim Speichern" };
        }

        await transaction.CommitAsync(cancellationToken);

        return null;
    }

    public async Task<string?> ConfirmChangeEmail(Guid id, string token, string encodedEmail)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return null;
        }

        string? newEmail = null;

        try
        {
            var protector = _dataProtectionProvider.CreateProtector(user.SecurityStamp!);

            newEmail = Encoding.UTF8.GetString(protector.Unprotect(Convert.FromBase64String(encodedEmail)));

            token = HttpUtility.UrlDecode(token);

            var isUserTokenValid = await _userManager.VerifyUserTokenAsync(user,
                _userManager.Options.Tokens.ChangeEmailTokenProvider,
                UserManager<IdentityUserGuid>.GetChangeEmailTokenPurpose(newEmail),
                token);

            if (!isUserTokenValid)
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change email for user {Email} failed", user.Email);
            return null;
        }

        var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            var error = string.Join(";", result.Errors.Select(e => e.Description));
            _logger.LogError("Change email for user {Email} failed: {Error}", user.Email, error);

            return null;
        }

        return newEmail;
    }

    private async Task<bool> NotifyConfirmRegistration(IdentityUserGuid user, CancellationToken cancellationToken)
    {
        if (_httpContext.HttpContext is null)
        {
            return false;
        }

        var dbSetAccountNotification = _dbContext.Set<AccountNotification>();
        var template = (int)AccountEmailTemplate.ConfirmRegistration;

        var entity = await dbSetAccountNotification
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedOn)
            .FirstOrDefaultAsync(e => e.UserId == user.Id && e.Type == template, cancellationToken);

        if (entity != null && !entity.SentOn.HasValue)
        {
            _logger.LogInformation("Notification for user {Email} is pending", user.Email);
            return true;
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        token = HttpUtility.UrlEncode(token);

        var callbackUrl = _linkGenerator.GetUriByPage(_httpContext.HttpContext, "/Login/ConfirmRegistration", null, new { id = user.Id, token });

        entity = new AccountNotification
        {
            Id = _pkGenerator.Generate(),
            UserId = user.Id,
            Type = (int)AccountEmailTemplate.ConfirmRegistration,
            CreatedOn = DateTimeOffset.UtcNow,
            CallbackUrl = callbackUrl
        };

        await dbSetAccountNotification.AddAsync(entity, cancellationToken);

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            _logger.LogInformation("Save changes for user {Email} failed", user.Email);
            return false;
        }
        return true;
    }

    private async Task<bool> NotifyChangePassword(IdentityUserGuid user, CancellationToken cancellationToken)
    {
        if (_httpContext.HttpContext is null)
        {
            return false;
        }

        var dbSetAccountNotification = _dbContext.Set<AccountNotification>();
        var template = AccountEmailTemplate.ResetPassword;

        var entity = await dbSetAccountNotification
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedOn)
            .FirstOrDefaultAsync(e => e.UserId == user.Id && e.Type == (int)template, cancellationToken);

        if (entity != null && !entity.SentOn.HasValue)
        {
            _logger.LogInformation("Notification for user {Email} is pending", user.Email);
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        token = HttpUtility.UrlEncode(token);

        var callbackUrl = _linkGenerator.GetUriByPage(_httpContext.HttpContext, "/Login/ConfirmChangePassword", null, new { id = user.Id, token });

        entity = new AccountNotification
        {
            Id = _pkGenerator.Generate(),
            UserId = user.Id,
            Type = (int)template,
            CreatedOn = DateTimeOffset.UtcNow,
            CallbackUrl = callbackUrl
        };

        await dbSetAccountNotification.AddAsync(entity, cancellationToken);

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            _logger.LogInformation("Save changes for user {Email} failed", user.Email);
            return false;
        }
        return true;
    }

    private async Task<bool> NotifyConfirmChangeEmail(IdentityUserGuid user, string newEmail, CancellationToken cancellationToken)
    {
        if (_httpContext.HttpContext is null)
        {
            return false;
        }

        var template = AccountEmailTemplate.ChangeEmail;
        var dbSetAccountNotification = _dbContext.Set<AccountNotification>();

        var currentNotify = await dbSetAccountNotification
            .OrderByDescending(e => e.CreatedOn)
            .FirstOrDefaultAsync(e => e.UserId == user.Id && e.Type == (int)template, cancellationToken);

        if (currentNotify != null && !currentNotify.SentOn.HasValue)
        {
            currentNotify.SentOn = DateTimeOffset.MinValue;
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        token = HttpUtility.UrlEncode(token);

        var protector = _dataProtectionProvider.CreateProtector(user.SecurityStamp!);
        var newEmailProtected = Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(newEmail)));

        var callbackUrl = _linkGenerator.GetUriByPage(_httpContext.HttpContext,
            "/Login/ConfirmChangeEmail", null,
            new { id = user.Id, token, email = newEmailProtected });

        var newNotify = new AccountNotification
        {
            Id = _pkGenerator.Generate(),
            UserId = user.Id,
            Type = (int)template,
            CreatedOn = DateTimeOffset.UtcNow,
            CallbackUrl = callbackUrl
        };

        await dbSetAccountNotification.AddAsync(newNotify, cancellationToken);

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            _logger.LogInformation("Save changes for user {Email} failed", user.Email);
            return false;
        }

        return true;
    }
}

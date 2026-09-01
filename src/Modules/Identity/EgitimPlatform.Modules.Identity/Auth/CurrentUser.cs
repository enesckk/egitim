using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Identity.Auth;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private Guid? _cachedInstitutionId;
    private bool _institutionResolved;

    public CurrentUser(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var claims = _httpContextAccessor.HttpContext?.User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value) ?? Enumerable.Empty<string>();
            return claims.ToList().AsReadOnly();
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

    public bool IsSuperAdmin => Roles.Contains(BuildingBlocks.Constants.Roles.SuperAdmin);

    public bool IsInRole(string role) => Roles.Contains(role);

    public async Task<Guid?> GetInstitutionIdAsync()
    {
        if (_institutionResolved) return _cachedInstitutionId;

        if (UserId is null) return null;

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == UserId.Value)
            .Select(u => new { u.InstitutionId })
            .FirstOrDefaultAsync();

        _cachedInstitutionId = user?.InstitutionId;
        _institutionResolved = true;
        return _cachedInstitutionId;
    }
}

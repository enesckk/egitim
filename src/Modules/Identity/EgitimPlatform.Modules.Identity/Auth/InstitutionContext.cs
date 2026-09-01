using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Identity.Auth;

public class InstitutionContext : IInstitutionContext
{
    private readonly ICurrentUser _currentUser;
    private Guid? _institutionId;
    private bool _resolved;

    public InstitutionContext(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public bool IsSuperAdmin => _currentUser.IsSuperAdmin;

    public Guid? InstitutionId
    {
        get
        {
            if (!_resolved)
            {
                _institutionId = _currentUser.GetInstitutionIdAsync().GetAwaiter().GetResult();
                _resolved = true;
            }
            return _institutionId;
        }
    }

    public bool IsResolved => _resolved;
}

using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;

using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.GetStudent;

public class GetStudentHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public GetStudentHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICoachStudentQuery coachStudentQuery)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task<StudentDto> HandleAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await EgitimPlatform.Modules.Students.Services.StudentReadAccess.GetAsync(
            _dbContext, _currentUser, _coachStudentQuery, studentId, ct);

        return student.ToDto();
    }
}

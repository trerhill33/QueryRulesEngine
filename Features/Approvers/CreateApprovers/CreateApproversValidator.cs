using FluentValidation;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Features.Approvers.CreateApprovers;

public sealed class CreateApproversValidator : AbstractValidator<CreateApproversRequest>
{
    private readonly IHierarchyRepository _hierarchyRepository;
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;

    public CreateApproversValidator(
            IHierarchyRepository hierarchyRepository,
            IReadOnlyRepositoryAsync<int> readOnlyRepository)
    {
        _hierarchyRepository = hierarchyRepository;
        _readOnlyRepository = readOnlyRepository;

        RuleFor(x => x.HierarchyId)
            .MustAsync(HierarchyExistsAsync)
            .WithMessage("Hierarchy does not exist");

        RuleFor(x => x.EmployeeTMIds)
            .NotEmpty()
            .WithMessage("Must provide at least one employee")
            .MustAsync(AllEmployeesExistAsync)
            .WithMessage("One or more employees do not exist")
            .MustAsync(NoExistingApproversAsync)
            .WithMessage("One or more employees are already approvers for this hierarchy");
    }

    private async Task<bool> HierarchyExistsAsync(
        int hierarchyId,
        CancellationToken cancellationToken)
            => await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);

    private async Task<bool> AllEmployeesExistAsync(
        CreateApproversRequest request,
        IEnumerable<string> tmids,
        CancellationToken cancellationToken)
    {
        var existingEmployees = await _readOnlyRepository
            .FindAllByPredicateAndTransformAsync<Employee, string>(
                e => tmids.Contains(e.TMID),
                e => e.TMID,
                cancellationToken);

        return existingEmployees.Count == tmids.Count();
    }

    private async Task<bool> NoExistingApproversAsync(
        CreateApproversRequest request,
        IEnumerable<string> tmids,
        CancellationToken cancellationToken)
    {
        var existingApprovers = await _readOnlyRepository
            .FindAllByPredicateAndTransformAsync<Approver, string>(
                a => a.HierarchyId == request.HierarchyId && tmids.Contains(a.ApproverId),
                a => a.ApproverId,
                cancellationToken);

        return !existingApprovers.Any();
    }
}

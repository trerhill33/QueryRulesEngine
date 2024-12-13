using FluentValidation;
using QueryRulesEngine.Repositories.Interfaces;

public sealed class GetHierarchyDetailsValidator : AbstractValidator<GetHierarchyDetailsRequest>
{
    private readonly IHierarchyRepository _repository;

    public GetHierarchyDetailsValidator(IHierarchyRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.HierarchyId)
            .MustAsync(HierarchyExistsAsync)
            .WithMessage("Hierarchy does not exist.");
    }

    private async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken)
        => await _repository.HierarchyExistsAsync(hierarchyId, cancellationToken);
}

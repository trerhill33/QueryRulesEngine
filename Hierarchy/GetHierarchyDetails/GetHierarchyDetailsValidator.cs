using FluentValidation;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Entities;
using System.Threading;
using System.Threading.Tasks;

public sealed class GetHierarchyDetailsValidator : AbstractValidator<GetHierarchyDetailsRequest>
{
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;

    public GetHierarchyDetailsValidator(IReadOnlyRepositoryAsync<int> readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository;

        RuleFor(x => x.HierarchyId)
            .MustAsync(HierarchyExists)
            .WithMessage("Hierarchy does not exist.");
    }

    private async Task<bool> HierarchyExists(int hierarchyId, CancellationToken cancellationToken)
    {
        return await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
            h => h.Id == hierarchyId,
            h => true,
            cancellationToken);
    }
}

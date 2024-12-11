using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

public sealed class AddMetadataKeyValidator : AbstractValidator<AddMetadataKeyRequest>
{
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;

    public AddMetadataKeyValidator(IReadOnlyRepositoryAsync<int> readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository;

        RuleFor(x => x.HierarchyId)
            .MustAsync(HierarchyExists)
            .WithMessage("Hierarchy does not exist.");

        RuleFor(x => x.KeyName)
            .NotEmpty()
            .WithMessage("Key name is required")
            .MustAsync(BeUniqueKeyForHierarchy)
            .WithMessage("Key already exists for this hierarchy");
    }

    private async Task<bool> HierarchyExists(int hierarchyId, CancellationToken cancellationToken)
    {
        return await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
            h => h.Id == hierarchyId,
            h => true,
            cancellationToken);
    }

    private async Task<bool> BeUniqueKeyForHierarchy(AddMetadataKeyRequest request, string keyName, CancellationToken cancellationToken)
    {
        return !(await _readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
            mk => mk.HierarchyId == request.HierarchyId && mk.KeyName == keyName,
            mk => true,
            cancellationToken));
    }
}

using FluentValidation;
using QueryRulesEngine.Repositories;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Hierarchys.AddMetadataKey
{
    public sealed class AddApproverMetadataKeyValidator : AbstractValidator<AddApproverMetadataKeyRequest>
    {
        private readonly IHierarchyRepository _hierarchyRepository;
        private readonly IApproverMetadataRepository _approverrepository;

        public AddApproverMetadataKeyValidator(IHierarchyRepository hierarchyRepository, IApproverMetadataRepository approverrepository)
        {
            _hierarchyRepository = hierarchyRepository;
            _approverrepository = approverrepository;

            RuleFor(x => x.HierarchyId)
                .MustAsync(HierarchyExistsAsync)
                .WithMessage("Hierarchy does not exist.");

            RuleFor(x => x.KeyName)
                .NotEmpty()
                .WithMessage("Key name is required")
                .MustAsync(BeUniqueKeyForHierarchy)
                .WithMessage("Key already exists for this hierarchy");
        }

        private async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken)
            => await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);

        private async Task<bool> BeUniqueKeyForHierarchy(AddApproverMetadataKeyRequest request, string keyName, CancellationToken cancellationToken)
            => await _approverrepository.IsUniqueKeyForHierarchyAsync(request.HierarchyId, keyName, cancellationToken);
    }
}
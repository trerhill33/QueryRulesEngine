using FluentValidation;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.MetadataKeys.RemoveApproverMetadataKey
{
    public sealed class RemoveApproverMetadataKeyValidator : AbstractValidator<RemoveApproverMetadataKeyRequest>
    {
        private readonly IHierarchyRepository _hierarchyRepository;
        private readonly IApproverMetadataRepository _approverMetadataRepository;

        public RemoveApproverMetadataKeyValidator(
            IHierarchyRepository hierarchyRepository,
            IApproverMetadataRepository approverMetadataRepository)
        {
            _hierarchyRepository = hierarchyRepository;
            _approverMetadataRepository = approverMetadataRepository;

            RuleFor(x => x.HierarchyId)
                .MustAsync(HierarchyExistsAsync)
                .WithMessage("Hierarchy does not exist.");

            RuleFor(x => x)
                .MustAsync(MetadataKeyExistsAsync)
                .WithMessage("Metadata key does not exist for this hierarchy.");
        }

        private async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken)
            => await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);

        private async Task<bool> MetadataKeyExistsAsync(
            RemoveApproverMetadataKeyRequest request,
            CancellationToken cancellationToken)
                => await _approverMetadataRepository.IsUniqueKeyForHierarchyAsync(
                    request.HierarchyId,
                    request.KeyName,
                    cancellationToken);
    }
}

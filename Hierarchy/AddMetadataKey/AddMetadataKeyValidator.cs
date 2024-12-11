using FluentValidation;
using QueryRulesEngine.Repositories;

namespace QueryRulesEngine.Hierarchys.AddMetadataKey
{
    public sealed class AddMetadataKeyValidator : AbstractValidator<AddMetadataKeyRequest>
    {
        private readonly IHierarchyRepository _repository;

        public AddMetadataKeyValidator(IHierarchyRepository repository)
        {
            _repository = repository;

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
            => await _repository.HierarchyExistsAsync(hierarchyId, cancellationToken);

        private async Task<bool> BeUniqueKeyForHierarchy(AddMetadataKeyRequest request, string keyName, CancellationToken cancellationToken)
            => await _repository.IsUniqueMetadataKeyNameAsync(request.HierarchyId, keyName, cancellationToken);
    }
}
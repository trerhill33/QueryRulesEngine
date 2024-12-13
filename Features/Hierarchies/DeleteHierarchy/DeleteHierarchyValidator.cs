using FluentValidation;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Features.Hierarchies.DeleteHierarchy
{
    public class DeleteHierarchyValidator : AbstractValidator<DeleteHierarchyRequest>
    {
        private readonly IHierarchyRepository _hierarchyRepository;

        public DeleteHierarchyValidator(IHierarchyRepository hierarchyRepository)
        {
            _hierarchyRepository = hierarchyRepository;

            RuleFor(x => x.HierarchyId)
                .NotEmpty()
                .MustAsync(HierarchyExistsAsync)
                    .WithMessage("Hierarchy does not exist.");
        }

        private async Task<bool> HierarchyExistsAsync(
            int hierarchyId,
            CancellationToken cancellationToken)
        {
            return await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);
        }
    }
}
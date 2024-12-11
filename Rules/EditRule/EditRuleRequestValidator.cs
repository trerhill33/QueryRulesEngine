using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories;

namespace QueryRulesEngine.Rules.EditRule
{
    public sealed class EditRuleRequestValidator : AbstractValidator<EditRuleRequest>
    {
        private readonly IHierarchyRepository _hierarchyRepository;
        private readonly IMetadataKeyRepository _metadataKeyRepository;

        public EditRuleRequestValidator(IHierarchyRepository hierarchyRepository, IMetadataKeyRepository metadataKeyRepository)
        {
            _hierarchyRepository = hierarchyRepository;
            _metadataKeyRepository = metadataKeyRepository;
            ConfigureValidationRules();
        }

        private void ConfigureValidationRules()
        {
            RuleFor(x => x.HierarchyId)
                .MustAsync(HierarchyExistsAsync)
                .WithMessage("Hierarchy does not exist.");

            RuleFor(x => x.LevelNumber)
                .MustAsync(LevelExistsAsync)
                .WithMessage("Level does not exist within the specified hierarchy.");

            RuleFor(x => x.RuleNumber)
                .NotEmpty()
                .WithMessage("Rule number is required.")
                .MustAsync(BeUniqueRuleNumberAsync)
                .WithMessage("A rule with this name already exists in the specified level.");

            RuleFor(x => x.QueryMatrix)
                .NotEmpty()
                .WithMessage("QueryMatrix is required.");
        }

        private async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken)
        {
            return await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);
        }

        private async Task<bool> LevelExistsAsync(EditRuleRequest request, int levelNumber, CancellationToken cancellationToken)
        {
            return await _hierarchyRepository.LevelExistsAsync(request.HierarchyId, levelNumber, cancellationToken);
        }

        private async Task<bool> BeUniqueRuleNumberAsync(EditRuleRequest request, string ruleNumber, CancellationToken cancellationToken)
        {
            return await _hierarchyRepository.IsUniqueRuleNumberAsync(request.HierarchyId, request.LevelNumber, ruleNumber, cancellationToken);
        }
    }
}

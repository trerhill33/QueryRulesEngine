using FluentValidation;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Features.Rules.AddRuleToLevel
{
    public sealed class AddRuleToLevelValidator : AbstractValidator<AddRuleToLevelRequest>
    {
        private readonly IHierarchyRepository _hierarchyRepository;
        private readonly IRuleRepository _ruleRepository;
        private readonly ILevelRepository _levelRepository;

        public AddRuleToLevelValidator(
            IHierarchyRepository hierarchyRepository,
            IRuleRepository ruleRepository,
            ILevelRepository levelRepository)
        {
            _hierarchyRepository = hierarchyRepository;
            _ruleRepository = ruleRepository;
            _levelRepository = levelRepository;
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
            => await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);

        private async Task<bool> LevelExistsAsync(AddRuleToLevelRequest request, int levelNumber, CancellationToken cancellationToken)
            => await _levelRepository.LevelExistsAsync(request.HierarchyId, levelNumber, cancellationToken);

        private async Task<bool> BeUniqueRuleNumberAsync(AddRuleToLevelRequest request, string ruleNumber, CancellationToken cancellationToken)
            => await _ruleRepository.ExistsAsync(new RuleDto()
            {
                HierarchyId = request.HierarchyId,
                RuleNumber = request.RuleNumber,
                LevelNumber = request.LevelNumber,
                QueryMatrix = request.QueryMatrix,
            },
                cancellationToken);

    }
}

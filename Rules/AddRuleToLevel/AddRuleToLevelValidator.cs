using FluentValidation;
using QueryRulesEngine.Repositories;

namespace QueryRulesEngine.Rules.AddRuleToLevel
{
    public sealed class AddRuleToLevelValidator : AbstractValidator<AddRuleToLevelRequest>
    {
        private readonly IHierarchyRepository _repository;

        public AddRuleToLevelValidator(IHierarchyRepository repository)
        {
            _repository = repository;
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
            => await _repository.HierarchyExistsAsync(hierarchyId, cancellationToken);

        private async Task<bool> LevelExistsAsync(AddRuleToLevelRequest request, int levelNumber, CancellationToken cancellationToken)
            => await _repository.LevelExistsAsync(request.HierarchyId, levelNumber, cancellationToken);

        private async Task<bool> BeUniqueRuleNumberAsync(AddRuleToLevelRequest request, string ruleNumber, CancellationToken cancellationToken)
            => await _repository.IsUniqueRuleNumberAsync(request.HierarchyId, request.LevelNumber, ruleNumber, cancellationToken);

    }
}

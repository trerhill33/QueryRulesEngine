using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Rules.EditRule
{

    public sealed class EditRuleRequestValidator : AbstractValidator<EditRuleRequest>
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;

        public EditRuleRequestValidator(IReadOnlyRepositoryAsync<int> readOnlyRepository)
        {
            _readOnlyRepository = readOnlyRepository;
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
            return await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Id == hierarchyId,
                h => true,
                cancellationToken);
        }

        private async Task<bool> LevelExistsAsync(
            EditRuleRequest request,
            int LevelNumber,
            CancellationToken cancellationToken)
        {
            var levelKey = $"level.{LevelNumber}";
            var exists = await _readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
                mk => mk.HierarchyId == request.HierarchyId && mk.KeyName == levelKey,
                mk => true,
                cancellationToken);
            return exists;
        }

        private async Task<bool> BeUniqueRuleNumberAsync(
            EditRuleRequest request,
            string RuleNumber,
            CancellationToken cancellationToken)
        {
            var rules = await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == request.HierarchyId &&
                      mk.KeyName.StartsWith($"level.{request.LevelNumber}.rule."),
                mk => mk.KeyName,
                cancellationToken);

            return !rules.Any(r => r.Contains(RuleNumber));
        }
    }
}

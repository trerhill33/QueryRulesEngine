using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Rules.RemoveRule
{
    public class RemoveRuleRequestValidator : AbstractValidator<RemoveRuleRequest>
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;

        public RemoveRuleRequestValidator(IReadOnlyRepositoryAsync<int> readOnlyRepository)
        {
            _readOnlyRepository = readOnlyRepository;
            ConfigureValidationRules();
        }

        public void ConfigureValidationRules()
        {
            RuleFor(x => x.HierarchyId)
                .MustAsync(HierarchyExistsAsync)
                .WithMessage("Hierarchy does not exist.");

            RuleFor(x => x.LevelNumber)
                .MustAsync(LevelExistsAsync)
                .WithMessage("Level does not exist within the specified hierarchy.");

            RuleFor(x => x.RuleNumber)
                .NotEmpty()
                .WithMessage("Rule number is required.");
        }

        private async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken)
        {
            return await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Id == hierarchyId,
                h => true,
                cancellationToken);
        }

        private async Task<bool> LevelExistsAsync(
            RemoveRuleRequest request,
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
    }
}

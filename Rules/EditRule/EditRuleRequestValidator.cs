using FluentValidation;
using QueryRulesEngine.dtos;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.Repositories.Interfaces;
using QueryRulesEngine.Rules.EditRule;

public sealed class EditRuleRequestValidator : AbstractValidator<EditRuleRequest>
{
    private readonly IHierarchyRepository _hierarchyRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly ILevelRepository _levelRepository;

    public EditRuleRequestValidator(
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
            .MustAsync(RuleExistsAsync)
            .WithMessage("Rule does not exist in the specified level.");

        RuleFor(x => x.QueryMatrix)
            .NotEmpty()
            .WithMessage("QueryMatrix is required.")
            .Must(HaveValidConditions)
            .WithMessage("QueryMatrix must contain at least one condition.");
    }

    private async Task<bool> HierarchyExistsAsync(
        int hierarchyId,
        CancellationToken cancellationToken)
            => await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);

    private async Task<bool> LevelExistsAsync(
        EditRuleRequest request,
        int levelNumber,
        CancellationToken cancellationToken)
            => await _levelRepository.LevelExistsAsync(
                request.HierarchyId,
                levelNumber,
                cancellationToken);

    private async Task<bool> RuleExistsAsync(
        EditRuleRequest request,
        string ruleNumber,
        CancellationToken cancellationToken)
            => await _ruleRepository.ExistsAsync(
                  new RuleDto()
                  {
                      HierarchyId = request.HierarchyId,
                      RuleNumber = request.RuleNumber,
                      LevelNumber = request.LevelNumber,
                      QueryMatrix = request.QueryMatrix,
                  },
                cancellationToken);

    private bool HaveValidConditions(QueryMatrix queryMatrix)
        => queryMatrix.Conditions.Count != 0 || queryMatrix.NestedMatrices.Count != 0;
}
using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;

namespace QueryRulesEngine.Features.Rules.RemoveRule
{
    public sealed class RemoveRuleService(
        IUnitOfWork<int> unitOfWork,
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IValidator<RemoveRuleRequest> validator) : IRemoveRuleService
    {
        private readonly IUnitOfWork<int> _unitOfWork = unitOfWork;
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
        private readonly IValidator<RemoveRuleRequest> _validator = validator;

        public async Task<Result<RemoveRuleResponse>> ExecuteAsync(RemoveRuleRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate the request
                var validationResult = await ValidateRequestAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                // 2. Fetch the existing rule using the correct repository method
                var rule = await _readOnlyRepository.FindByPredicateAsync<MetadataKey>(
                    mk =>
                          mk.HierarchyId == request.HierarchyId &&
                          mk.KeyName.StartsWith($"level.{request.LevelNumber}.rule.{request.RuleNumber}.query:"),
                    cancellationToken);

                if (rule == null)
                {
                    return await Result<RemoveRuleResponse>.FailAsync(
                        "Rule does not exist within the specified hierarchy.",
                         ResultStatus.Error);
                }

                // 3. Remove the rule
                await _unitOfWork.Repository<MetadataKey>().DeleteAsync(rule);
                await _unitOfWork.CommitAsync(cancellationToken);

                return await Result<RemoveRuleResponse>.SuccessAsync(new RemoveRuleResponse(
                    RuleId: rule.Id,
                    KeyName: rule.KeyName
                ), ResultStatus.Success);
            }
            catch (Exception ex)
            {
                return await Result<RemoveRuleResponse>.FailAsync($"Error removing rule: {ex.Message}", ResultStatus.Error);
            }
        }

        private async Task<ValidationResult> ValidateRequestAsync(RemoveRuleRequest request, CancellationToken cancellationToken)
        {
            return await _validator.ValidateAsync(request, cancellationToken);
        }

        private async Task<Result<RemoveRuleResponse>> HandleValidationFailureAsync(ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<RemoveRuleResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}

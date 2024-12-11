using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;
using QueryRulesEngine.Rules.EditRule;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueryRulesEngine.Hierarchys.EditRule
{
    public sealed class EditRuleService(
        IUnitOfWork<int> unitOfWork,
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IValidator<EditRuleRequest> validator,
        IQueryPersistenceService queryPersistenceService) : IEditRuleService
    {
        private readonly IUnitOfWork<int> _unitOfWork = unitOfWork;
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
        private readonly IValidator<EditRuleRequest> _validator = validator;
        private readonly IQueryPersistenceService _queryPersistenceService = queryPersistenceService;

        public async Task<Result<EditRuleResponse>> ExecuteAsync(EditRuleRequest request, CancellationToken cancellationToken = default)
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
                    return await Result<EditRuleResponse>.FailAsync(
                        "Rule not found.", 
                        ResultStatus.Error);
                }

                // 3. Update the rule
                UpdateRule(rule, request);

                // 4. Save changes
                await _unitOfWork.Repository<MetadataKey>().UpdateAsync(rule);
                await _unitOfWork.CommitAsync(cancellationToken);

                // 5. Build and return the response using constructor-based instantiation
                var response = new EditRuleResponse(
                    RuleId: rule.Id,
                    KeyName: rule.KeyName,
                    UpdatedQueryMatrix: request.QueryMatrix
                );

                return await Result<EditRuleResponse>.SuccessAsync(response, ResultStatus.Success);
            }
            catch (Exception ex)
            {
                return await Result<EditRuleResponse>.FailAsync($"Error editing rule: {ex.Message}", ResultStatus.Error);
            }
        }

        private async Task<ValidationResult> ValidateRequestAsync(EditRuleRequest request, CancellationToken cancellationToken)
        {
            return await _validator.ValidateAsync(request, cancellationToken);
        }

        private async Task<Result<EditRuleResponse>> HandleValidationFailureAsync(ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<EditRuleResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }

        private void UpdateRule(MetadataKey rule, EditRuleRequest request)
        {
            var persistedQuery = _queryPersistenceService.ConvertToStorageFormat(request.QueryMatrix);
            rule.KeyName = $"level.{request.LevelNumber}.rule.{request.RuleNumber}.query:{persistedQuery}";
        }
    }
}

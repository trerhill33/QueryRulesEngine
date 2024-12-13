using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Rules.EditRule
{
    public sealed class EditRuleService(
        IRuleRepository ruleRepository,
        ILevelRepository levelRepository,
        IValidator<EditRuleRequest> validator) : IEditRuleService
    {
        public async Task<Result<EditRuleResponse>> ExecuteAsync(
            EditRuleRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate the request
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                // 2. Fetch the existing rule
                var existingRule = await ruleRepository.GetRuleAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    request.RuleNumber,
                    cancellationToken);

                if (existingRule == null)
                {
                    return await Result<EditRuleResponse>.FailAsync(
                        "Rule not found.",
                        ResultStatus.Error);
                }

                // 3. Extract and validate metadata keys from QueryMatrix
                var metadataKeyNames = ExtractMetadataKeyNames(request.QueryMatrix);
                if (metadataKeyNames.Any())
                {
                    await ValidateMetadataKeysExistAsync(
                        request.HierarchyId,
                        metadataKeyNames,
                        cancellationToken);
                }

                // 4. Update the rule
                existingRule.QueryMatrix = request.QueryMatrix;
                await ruleRepository.UpdateRuleAsync(
                    existingRule,
                    cancellationToken);

                return await Result<EditRuleResponse>.SuccessAsync(ResultStatus.Success);
            }
            catch (Exception ex)
            {
                return await Result<EditRuleResponse>.FailAsync(
                    $"Error editing rule: {ex.Message}",
                    ResultStatus.Error);
            }
        }

        private async Task<Result<EditRuleResponse>> HandleValidationFailureAsync(
            ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<EditRuleResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }

        private List<string> ExtractMetadataKeyNames(QueryMatrix queryMatrix)
        {
            var metadataKeyNames = new List<string>();

            foreach (var condition in queryMatrix.Conditions)
            {
                if (condition.Field.StartsWith("ApproverMetadataKey."))
                {
                    metadataKeyNames.Add(condition.Field);
                }
            }

            foreach (var nestedMatrix in queryMatrix.NestedMatrices)
            {
                metadataKeyNames.AddRange(ExtractMetadataKeyNames(nestedMatrix));
            }

            return metadataKeyNames;
        }

        private async Task ValidateMetadataKeysExistAsync(
            int hierarchyId,
            IEnumerable<string> metadataKeyNames,
            CancellationToken cancellationToken)
        {
            var existingKeys = await ruleRepository.GetExistingMetadataKeysAsync(
                hierarchyId,
                metadataKeyNames,
                cancellationToken);

            var missingKeys = metadataKeyNames.Except(existingKeys).ToList();
            if (missingKeys.Any())
            {
                throw new ValidationException(
                    $"The following metadata keys do not exist: {string.Join(", ", missingKeys)}");
            }
        }
    }
}

using FluentValidation;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Features.Rules.AddRuleToLevel
{
    public sealed class AddRuleToLevelService(
        IRuleRepository ruleRepository,
        ILevelRepository levelRepository,
        IValidator<AddRuleToLevelRequest> validator) : IAddRuleToLevelService
    {

        private readonly IRuleRepository _ruleRepository = ruleRepository;
        private readonly ILevelRepository _levelRepository = levelRepository;
        private readonly IValidator<AddRuleToLevelRequest> _validator = validator;

        public async Task<Result<AddRuleToLevelResponse>> ExecuteAsync(
            AddRuleToLevelRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate the request
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                // 2. Extract metadata key names from QueryMatrix and validate they exist
                var metadataKeyNames = ExtractMetadataKeyNames(request.QueryMatrix);
                if (metadataKeyNames.Count != 0)
                {
                    await ValidateMetadataKeysExistAsync(request.HierarchyId, metadataKeyNames, cancellationToken);
                }

                // 3. Get next rule number if not provided
                var ruleNumber = await _ruleRepository.GetNextRuleNumberAsync(
                    request.HierarchyId,
                    request.LevelNumber,
                    cancellationToken);


                // 4. Create the rule
                await _ruleRepository.CreateRuleAsync(
                    new dtos.RuleDto()
                    {
                        HierarchyId = request.HierarchyId,
                        RuleNumber = ruleNumber.ToString(),
                        LevelNumber = request.LevelNumber,
                        QueryMatrix = request.QueryMatrix,
                    },
                    cancellationToken);

                // 5. Build and return success response
                return await Result<AddRuleToLevelResponse>.SuccessAsync(
                    new AddRuleToLevelResponse
                    {
                        HierarchyId = request.HierarchyId,
                        LevelNumber = request.LevelNumber,
                        RuleNumber = ruleNumber.ToString(),
                        QueryMatrix = request.QueryMatrix
                    });
            }
            catch (Exception ex)
            {
                return await Result<AddRuleToLevelResponse>.FailAsync(
                    $"Error adding rule to level: {ex.Message}",
                    ResultStatus.Error);
            }
        }

        private async Task<Result<AddRuleToLevelResponse>> HandleValidationFailureAsync(
            FluentValidation.Results.ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<AddRuleToLevelResponse>.FailAsync(errorMessages, ResultStatus.Error);
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
            var existingKeys = await _ruleRepository.GetExistingMetadataKeysAsync(
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
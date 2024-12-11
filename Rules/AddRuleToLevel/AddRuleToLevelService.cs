using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;

namespace QueryRulesEngine.Rules.AddRuleToLevel
{
    public sealed class AddRuleToLevelService : IAddRuleToLevelService
    {
        private readonly IUnitOfWork<int> _unitOfWork;
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;
        private readonly IValidator<AddRuleToLevelRequest> _validator;
        private readonly IQueryPersistenceService _queryPersistenceService;

        public AddRuleToLevelService(
            IUnitOfWork<int> unitOfWork,
            IReadOnlyRepositoryAsync<int> readOnlyRepository,
            IValidator<AddRuleToLevelRequest> validator,
            IQueryPersistenceService queryPersistenceService)
        {
            _unitOfWork = unitOfWork;
            _readOnlyRepository = readOnlyRepository;
            _validator = validator;
            _queryPersistenceService = queryPersistenceService;
        }

        public async Task<Result<AddRuleToLevelResponse>> ExecuteAsync(AddRuleToLevelRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate the request
                var validationResult = await ValidateRequestAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                // 2. Extract metadata key names from QueryMatrix
                var metadataKeyNames = ExtractMetadataKeyNames(request.QueryMatrix);

                // 3. Check if all metadata keys exist
                await CheckIfExistMetadataKeysAsync(request.HierarchyId, metadataKeyNames, cancellationToken);

                // 4. Create and persist the rule as a MetadataKey
                var ruleKey = await CreateRuleMetadataKeyAsync(request, cancellationToken);

                // 5. Build and return the success response
                return await BuildSuccessResponseAsync(request, ruleKey);
            }
            catch (Exception ex)
            {
                return await Result<AddRuleToLevelResponse>.FailAsync($"Error adding rule to level: {ex.Message}", ResultStatus.Error);
            }
        }

        #region Private Methods

        private async Task<ValidationResult> ValidateRequestAsync(AddRuleToLevelRequest request, CancellationToken cancellationToken)
        {
            return await _validator.ValidateAsync(request, cancellationToken);
        }

        private async Task<Result<AddRuleToLevelResponse>> HandleValidationFailureAsync(ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<AddRuleToLevelResponse>.FailAsync(errorMessages, ResultStatus.Error);
        }

        private List<string> ExtractMetadataKeyNames(QueryMatrix queryMatrix)
        {
            var metadataKeyNames = new List<string>();

            foreach (var condition in queryMatrix.Conditions)
            {
                if (condition.Field.StartsWith("MetadataKey."))
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

        private async Task CheckIfExistMetadataKeysAsync(int hierarchyId, IEnumerable<string> metadataKeyNames, CancellationToken cancellationToken)
        {
            var existingKeys = await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == hierarchyId && metadataKeyNames.Contains(mk.KeyName),
                mk => mk.KeyName,
                cancellationToken);

            var missingKeys = metadataKeyNames.Except(existingKeys).ToList();

            if (missingKeys.Any())
            {
                throw new ValidationException($"The following metadata keys do not exist: {string.Join(", ", missingKeys)}");
            }
        }

        private async Task<MetadataKey> CreateRuleMetadataKeyAsync(AddRuleToLevelRequest request, CancellationToken cancellationToken)
        {
            var nextRuleNumber = await GetNextRuleNumberAsync(request, cancellationToken);
            var persistedQuery = _queryPersistenceService.ConvertToStorageFormat(request.QueryMatrix);

            var ruleKeyName = $"level.{request.LevelNumber}.rule.{nextRuleNumber}.query:{persistedQuery}";

            var ruleKey = new MetadataKey
            {
                HierarchyId = request.HierarchyId,
                KeyName = ruleKeyName
            };

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var metadataKeyRepository = _unitOfWork.Repository<MetadataKey>();
                await metadataKeyRepository.AddAsync(ruleKey, cancellationToken);
            }, cancellationToken);

            return ruleKey;
        }

        // Determines the next rule number based on existing rules
        private async Task<int> GetNextRuleNumberAsync(AddRuleToLevelRequest request, CancellationToken cancellationToken)
        {
            var existingRules = await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == request.HierarchyId && mk.KeyName.StartsWith($"level.{request.LevelNumber}.rule."),
                mk => mk.KeyName,
                cancellationToken);

            if (existingRules.Count == 0)
            {
                return 1;
            }

            return existingRules
                .Select(r => ParseRuleNumber(r))
                .Max() + 1;
        }

        private int ParseRuleNumber(string keyName)
        {
            var parts = keyName.Split('.');
            if (parts.Length < 4)
            {
                throw new FormatException($"Invalid rule KeyName format: {keyName}");
            }

            if (int.TryParse(parts[3], out int ruleNumber))
            {
                return ruleNumber;
            }

            throw new FormatException($"Unable to parse rule number from KeyName: {keyName}");
        }

        private async Task<Result<AddRuleToLevelResponse>> BuildSuccessResponseAsync(AddRuleToLevelRequest request, MetadataKey ruleKey)
        {
            var response = new AddRuleToLevelResponse
            {
                RuleId = ruleKey.Id,
                HierarchyId = request.HierarchyId,
                LevelNumber = request.LevelNumber,
                RuleNumber = request.RuleNumber,
                QueryMatrix = request.QueryMatrix
            };

            return await Result<AddRuleToLevelResponse>.SuccessAsync(response, ResultStatus.Success);
        }

        #endregion
    }
}

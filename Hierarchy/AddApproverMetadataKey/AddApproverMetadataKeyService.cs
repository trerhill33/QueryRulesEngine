using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Hierarchys.AddApproverMetadataKey
{
    public sealed class AddApproverMetadataKeyService(
        IApproverMetadataRepository approverMetadataRepository,
        IHierarchyRepository hierarchyRepository,
        IValidator<AddApproverMetadataKeyRequest> validator) : IAddApproverMetadataKeyService
    {
        private readonly IApproverMetadataRepository _approverMetadataRepository = approverMetadataRepository;
        private readonly IHierarchyRepository _hierarchyRepository = hierarchyRepository;
        private readonly IValidator<AddApproverMetadataKeyRequest> _validator = validator;

        public async Task<Result<AddApproverMetadataKeyResponse>> ExecuteAsync(
            AddApproverMetadataKeyRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                await _approverMetadataRepository.CreateApproverMetadataKeyAsync(
                    request.HierarchyId,
                    request.KeyName,
                    cancellationToken);

                return await Result<AddApproverMetadataKeyResponse>.SuccessAsync(
                    new AddApproverMetadataKeyResponse
                    {
                        HierarchyId = request.HierarchyId,
                        KeyName = $"ApproverMetadataKey.{request.KeyName}"
                    });
            }
            catch (Exception ex)
            {
                return await Result<AddApproverMetadataKeyResponse>.FailAsync($"Error adding approver metadata key: {ex.Message}");
            }
        }

        private async Task<Result<AddApproverMetadataKeyResponse>> HandleValidationFailureAsync(
            FluentValidation.Results.ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<AddApproverMetadataKeyResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}
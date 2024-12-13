using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Hierarchys.MetadataKeys.AddApproverMetadataKey
{
    public sealed class AddApproverMetadataKeyService(
        IApproverMetadataRepository approverMetadataRepository,
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IUnitOfWork<int> unitOfWork,
        IValidator<AddApproverMetadataKeyRequest> validator) : IAddApproverMetadataKeyService
    {
        public async Task<Result<AddApproverMetadataKeyResponse>> ExecuteAsync(
            AddApproverMetadataKeyRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate request
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return await HandleValidationFailureAsync(validationResult);

                // 2. Create metadata key and records for existing approvers
                await CreateMetadataKeyAndRecordsAsync(request, cancellationToken);

                // 3. Return success response
                return await Result<AddApproverMetadataKeyResponse>.SuccessAsync(
                    new AddApproverMetadataKeyResponse
                    {
                        HierarchyId = request.HierarchyId,
                        KeyName = $"ApproverMetadataKey.{request.KeyName}"
                    });
            }
            catch (Exception ex)
            {
                return await Result<AddApproverMetadataKeyResponse>.FailAsync(
                    $"Error adding approver metadata key: {ex.Message}");
            }
        }

        private async Task CreateMetadataKeyAndRecordsAsync(
            AddApproverMetadataKeyRequest request,
            CancellationToken cancellationToken)
        {
            var existingApprovers = await GetExistingApproversAsync(request.HierarchyId, cancellationToken);

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await approverMetadataRepository.CreateApproverMetadataKeyAsync(
                    request.HierarchyId,
                    request.KeyName,
                    cancellationToken);

                await CreateMetadataRecordsForApproversAsync(
                    request.HierarchyId,
                    request.KeyName,
                    existingApprovers,
                    cancellationToken);
            }, cancellationToken);
        }

        private async Task<List<string>> GetExistingApproversAsync(
            int hierarchyId,
            CancellationToken cancellationToken)
        {
            return await readOnlyRepository
                .FindAllByPredicateAndTransformAsync<Approver, string>(
                    a => a.HierarchyId == hierarchyId,
                    a => a.ApproverId,
                    cancellationToken);
        }

        private async Task CreateMetadataRecordsForApproversAsync(
            int hierarchyId,
            string keyName,
            List<string> approverIds,
            CancellationToken cancellationToken)
        {
            var metadataRepo = unitOfWork.Repository<Metadata>();
            foreach (var approverId in approverIds)
            {
                var metadata = new Metadata
                {
                    HierarchyId = hierarchyId,
                    ApproverId = approverId,
                    Key = $"ApproverMetadataKey.{keyName}",
                    Value = null
                };
                await metadataRepo.AddAsync(metadata, cancellationToken);
            }
        }

        private async Task<Result<AddApproverMetadataKeyResponse>> HandleValidationFailureAsync(
            ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<AddApproverMetadataKeyResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}
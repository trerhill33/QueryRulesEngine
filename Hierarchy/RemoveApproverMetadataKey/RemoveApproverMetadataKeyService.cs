using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Hierarchys.RemoveApproverMetadataKey
{
    public sealed class RemoveApproverMetadataKeyService(
        IApproverMetadataRepository approverMetadataRepository,
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IUnitOfWork<int> unitOfWork,
        IValidator<RemoveApproverMetadataKeyRequest> validator) : IRemoveApproverMetadataKeyService
    {
        public async Task<Result<RemoveApproverMetadataKeyResponse>> ExecuteAsync(
            RemoveApproverMetadataKeyRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return await HandleValidationFailureAsync(validationResult);

                await RemoveMetadataKeyAndRecordsAsync(request, cancellationToken);

                return await Result<RemoveApproverMetadataKeyResponse>.SuccessAsync(
                    new RemoveApproverMetadataKeyResponse(
                        request.HierarchyId,
                        request.KeyName));
            }
            catch (Exception ex)
            {
                return await Result<RemoveApproverMetadataKeyResponse>.FailAsync(
                    $"Error removing approver metadata key: {ex.Message}");
            }
        }

        private async Task RemoveMetadataKeyAndRecordsAsync(
            RemoveApproverMetadataKeyRequest request,
            CancellationToken cancellationToken)
        {
            var keyName = $"ApproverMetadataKey.{request.KeyName}";
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await RemoveMetadataRecordsAsync(request.HierarchyId, keyName, cancellationToken);
                await RemoveMetadataKeyAsync(request.HierarchyId, keyName, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
            }, cancellationToken);
        }

        private async Task RemoveMetadataRecordsAsync(
            int hierarchyId,
            string keyName,
            CancellationToken cancellationToken)
        {
            var metadataToRemove = await readOnlyRepository.FindAllByPredicateAsync<Metadata>(
                m => m.HierarchyId == hierarchyId && m.Key == keyName,
                cancellationToken);

            var metadataRepo = unitOfWork.Repository<Metadata>();
            foreach (var metadata in metadataToRemove)
            {
                await metadataRepo.DeleteAsync(metadata);
            }
        }

        private async Task RemoveMetadataKeyAsync(
            int hierarchyId,
            string keyName,
            CancellationToken cancellationToken)
        {
            var key = await readOnlyRepository.FindByPredicateAsync<MetadataKey>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName == keyName,
                cancellationToken);

            if (key != null)
            {
                await unitOfWork.Repository<MetadataKey>().DeleteAsync(key);
            }
        }

        private async Task<Result<RemoveApproverMetadataKeyResponse>> HandleValidationFailureAsync(
            ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<RemoveApproverMetadataKeyResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}

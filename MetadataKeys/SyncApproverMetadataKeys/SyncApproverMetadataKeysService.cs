using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.MetadataKeys.SyncApproverMetadataKeys
{
    public sealed class SyncApproverMetadataKeysService(
        IApproverMetadataRepository approverMetadataRepository,
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IUnitOfWork<int> unitOfWork,
        IValidator<SyncApproverMetadataKeysRequest> validator) : ISyncApproverMetadataKeysService
    {
        public async Task<Result<SyncApproverMetadataKeysResponse>> ExecuteAsync(
            SyncApproverMetadataKeysRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return await HandleValidationFailureAsync(validationResult);

                var (recordsAdded, recordsRemoved, addedKeys, removedKeys) =
                    await SyncMetadataRecordsAsync(request.HierarchyId, cancellationToken);

                return await Result<SyncApproverMetadataKeysResponse>.SuccessAsync(
                    new SyncApproverMetadataKeysResponse(
                        HierarchyId: request.HierarchyId,
                        MetadataRecordsAdded: recordsAdded,
                        MetadataRecordsRemoved: recordsRemoved,
                        AddedKeys: addedKeys,
                        RemovedKeys: removedKeys));
            }
            catch (Exception ex)
            {
                return await Result<SyncApproverMetadataKeysResponse>.FailAsync(
                    $"Error syncing approver metadata keys: {ex.Message}");
            }
        }

        private async Task<(int recordsAdded, int recordsRemoved, List<string> addedKeys, List<string> removedKeys)>
            SyncMetadataRecordsAsync(int hierarchyId, CancellationToken cancellationToken)
        {
            var metadataKeys = await approverMetadataRepository.GetApproverMetadataKeysAsync(hierarchyId, cancellationToken);
            var approvers = await GetApproversAsync(hierarchyId, cancellationToken);
            var existingMetadata = await GetExistingMetadataAsync(hierarchyId, cancellationToken);

            return await ProcessMetadataUpdatesAsync(
                hierarchyId,
                metadataKeys,
                approvers,
                existingMetadata,
                cancellationToken);
        }

        private async Task<List<string>> GetApproversAsync(
            int hierarchyId,
            CancellationToken cancellationToken)
        {
            return await readOnlyRepository.FindAllByPredicateAndTransformAsync<Approver, string>(
                a => a.HierarchyId == hierarchyId,
                a => a.ApproverId,
                cancellationToken);
        }

        private async Task<List<Metadata>?> GetExistingMetadataAsync(
            int hierarchyId,
            CancellationToken cancellationToken)
        {
            return await readOnlyRepository.FindAllByPredicateAsync<Metadata>(
                m => m.HierarchyId == hierarchyId && m.Key.StartsWith("ApproverMetadataKey."),
                cancellationToken);
        }

        private async Task<(int recordsAdded, int recordsRemoved, List<string> addedKeys, List<string> removedKeys)>
            ProcessMetadataUpdatesAsync(
                int hierarchyId,
                List<string> metadataKeys,
                List<string> approvers,
                List<Metadata> existingMetadata,
                CancellationToken cancellationToken)
        {
            var recordsAdded = 0;
            var recordsRemoved = 0;
            var addedKeys = new List<string>();
            var removedKeys = new List<string>();

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var metadataRepo = unitOfWork.Repository<Metadata>();
                var existingByApprover = existingMetadata
                    .GroupBy(m => m.ApproverId)
                    .ToDictionary(g => g.Key, g => g.Select(m => m.Key).ToHashSet());

                foreach (var approverId in approvers)
                {
                    var approverKeys = existingByApprover.GetValueOrDefault(approverId, []);
                    var missingKeys = metadataKeys.Except(approverKeys).ToList();

                    foreach (var key in missingKeys)
                    {
                        await metadataRepo.AddAsync(new Metadata
                        {
                            HierarchyId = hierarchyId,
                            ApproverId = approverId,
                            Key = key,
                            Value = string.Empty
                        }, cancellationToken);

                        recordsAdded++;
                        if (!addedKeys.Contains(key))
                            addedKeys.Add(key);
                    }
                }

                foreach (var metadata in existingMetadata)
                {
                    if (!metadataKeys.Contains(metadata.Key))
                    {
                        await metadataRepo.DeleteAsync(metadata);
                        recordsRemoved++;
                        if (!removedKeys.Contains(metadata.Key))
                            removedKeys.Add(metadata.Key);
                    }
                }

                await unitOfWork.CommitAsync(cancellationToken);
            }, cancellationToken);

            return (recordsAdded, recordsRemoved, addedKeys, removedKeys);
        }

        private async Task<Result<SyncApproverMetadataKeysResponse>> HandleValidationFailureAsync(
            ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<SyncApproverMetadataKeysResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}

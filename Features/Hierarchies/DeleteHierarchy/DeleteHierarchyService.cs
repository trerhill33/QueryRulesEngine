using FluentValidation;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using System.Data;

namespace QueryRulesEngine.Features.Hierarchies.DeleteHierarchy
{
    public sealed class DeleteHierarchyService(
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IUnitOfWork<int> unitOfWork,
        IValidator<DeleteHierarchyRequest> validator)
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
        private readonly IUnitOfWork<int> _unitOfWork = unitOfWork;
        private readonly IValidator<DeleteHierarchyRequest> _validator = validator;

        public async Task<Result<DeleteHierarchyResponse>> ExecuteAsync(
            DeleteHierarchyRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate request
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await Result<DeleteHierarchyResponse>.FailAsync(
                        validationResult.Errors.Select(e => e.ErrorMessage).ToList());
                }

                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get all metadata records for this hierarchy
                    var metadataToDelete = await _readOnlyRepository
                        .FindAllByPredicateAsync<Metadata>(
                            m => m.HierarchyId == request.HierarchyId,
                            cancellationToken);

                    // Get all metadata keys for this hierarchy
                    var metadataKeysToDelete = await _readOnlyRepository
                        .FindAllByPredicateAsync<MetadataKey>(
                            mk => mk.HierarchyId == request.HierarchyId,
                            cancellationToken);

                    // Delete all metadata records in one operation
                    if (metadataToDelete is not null)
                    {
                        _unitOfWork.Repository<Metadata>().DeleteRange(metadataToDelete);
                    }

                    // Delete all metadata keys in one operation
                    if (metadataKeysToDelete is not null)
                    {
                        _unitOfWork.Repository<MetadataKey>().DeleteRange(metadataKeysToDelete);
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);
                }, cancellationToken);

                return Result<DeleteHierarchyResponse>.Success(new DeleteHierarchyResponse
                {
                    HierarchyId = request.HierarchyId,
                });
            }
            catch (Exception ex)
            {
                return await Result<DeleteHierarchyResponse>.FailAsync(
                    $"Error deleting hierarchy metadata: {ex.Message}",
                    ResultStatus.Error);
            }
        }
    }
}

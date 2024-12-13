using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Approvers.CreateApprovers
{
    public sealed class CreateApproversService(
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IUnitOfWork<int> unitOfWork,
        IValidator<CreateApproversRequest> validator) : ICreateApproversService
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
        private readonly IUnitOfWork<int> _unitOfWork = unitOfWork;
        private readonly IValidator<CreateApproversRequest> _validator = validator;

        public async Task<Result<CreateApproversResponse>> ExecuteAsync(
            CreateApproversRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate request
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                // 2. Get all metadata keys for this hierarchy
                var metadataKeys = await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                    mk => mk.HierarchyId == request.HierarchyId && mk.KeyName.StartsWith("ApproverMetadataKey."),
                    mk => mk.KeyName,
                    cancellationToken);

                // 3. Create approvers and their metadata records
                var createdTMIds = new List<string>();

                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    foreach (var tmid in request.EmployeeTMIds)
                    {
                        // Create approver record
                        var approver = new Approver
                        {
                            HierarchyId = request.HierarchyId,
                            ApproverId = tmid
                        };
                        await _unitOfWork.Repository<Approver>().AddAsync(approver, cancellationToken);

                        // Create metadata records for each key (initially null values)
                        foreach (var key in metadataKeys)
                        {
                            var metadata = new Metadata
                            {
                                HierarchyId = request.HierarchyId,
                                ApproverId = tmid,
                                Key = key,
                                Value = string.Empty // Initial value is null
                            };
                            await _unitOfWork.Repository<Metadata>().AddAsync(metadata, cancellationToken);
                        }

                        createdTMIds.Add(tmid);
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);
                }, cancellationToken);

                // 4. Return success response
                return await Result<CreateApproversResponse>.SuccessAsync(
                    new CreateApproversResponse(
                        HierarchyId: request.HierarchyId,
                        ApproversCreated: createdTMIds.Count,
                        CreatedApproverTMIds: createdTMIds));
            }
            catch (Exception ex)
            {
                return await Result<CreateApproversResponse>.FailAsync(
                    $"Error creating approvers: {ex.Message}",
                    ResultStatus.Error);
            }
        }

        private async Task<Result<CreateApproversResponse>> HandleValidationFailureAsync(
            ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<CreateApproversResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}

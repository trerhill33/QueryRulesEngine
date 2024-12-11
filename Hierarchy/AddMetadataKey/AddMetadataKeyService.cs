using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

public sealed class AddMetadataKeyService : IAddMetadataKeyService
{
    private readonly IUnitOfWork<int> _unitOfWork;
    private readonly IValidator<AddMetadataKeyRequest> _validator;

    public AddMetadataKeyService(
        IUnitOfWork<int> unitOfWork,
        IValidator<AddMetadataKeyRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<AddMetadataKeyResponse>> ExecuteAsync(
        AddMetadataKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await ValidateRequestAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return await HandleValidationFailureAsync(validationResult);
            }

            var metadataKey =
                new MetadataKey
                {
                    HierarchyId = request.HierarchyId,
                    KeyName = $"MetadataKey.{request.KeyName}"
                };

            await _unitOfWork.Repository<MetadataKey>().AddAsync(metadataKey, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return await Result<AddMetadataKeyResponse>.SuccessAsync(
                new AddMetadataKeyResponse
                {
                    HierarchyId = metadataKey.HierarchyId,
                    KeyName = metadataKey.KeyName
                });
        }
        catch (Exception ex)
        {
            return await Result<AddMetadataKeyResponse>.FailAsync($"Error adding metadata key: {ex.Message}");
        }
    }

    // Private method to validate the request
    private async Task<FluentValidation.Results.ValidationResult> ValidateRequestAsync(
        AddMetadataKeyRequest request,
        CancellationToken cancellationToken)
    {
        return await _validator.ValidateAsync(request, cancellationToken);
    }

    // Private method to handle validation failures
    private async Task<Result<AddMetadataKeyResponse>> HandleValidationFailureAsync(
        FluentValidation.Results.ValidationResult validationResult)
    {
        var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return await Result<AddMetadataKeyResponse>.FailAsync(
            errorMessages,
            ResultStatus.Error);
    }
}
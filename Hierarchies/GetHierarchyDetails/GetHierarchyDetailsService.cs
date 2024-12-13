using FluentValidation;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

public sealed class GetHierarchyDetailsService : IGetHierarchyDetailsService
{
    private readonly IUnitOfWork<int> _unitOfWork;
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private readonly IValidator<GetHierarchyDetailsRequest> _validator;

    public GetHierarchyDetailsService(
        IUnitOfWork<int> unitOfWork,
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IValidator<GetHierarchyDetailsRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _readOnlyRepository = readOnlyRepository;
        _validator = validator;
    }

    public async Task<Result<GetHierarchyDetailsResponse>> ExecuteAsync(GetHierarchyDetailsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return await Result<GetHierarchyDetailsResponse>.FailAsync(
                    errorMessages,
                    ResultStatus.Error);
            }

            // Retrieve the hierarchy
            var hierarchy = await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, Hierarchy>(
                h => h.Id == request.HierarchyId,
                h => h,
                cancellationToken);

            if (hierarchy == null)
            {
                return await Result<GetHierarchyDetailsResponse>.FailAsync("Hierarchy not found.", ResultStatus.NotFound);
            }

            // Retrieve associated metadata keys
            var metadataKeys = await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
                mk => mk.HierarchyId == request.HierarchyId,
                mk => mk,
                cancellationToken);

            var response = new GetHierarchyDetailsResponse
            {
                Id = hierarchy.Id,
                Name = hierarchy.Name,
                Description = hierarchy.Description,
                MetadataKeys = metadataKeys.Select(mk => new MetadataKeyDto
                {
                    Id = mk.Id,
                    KeyName = mk.KeyName
                }).ToList()
            };

            return await Result<GetHierarchyDetailsResponse>.SuccessAsync(response, ResultStatus.Success);
        }
        catch (Exception ex)
        {
            return await Result<GetHierarchyDetailsResponse>.FailAsync($"Error retrieving hierarchy details: {ex.Message}", ResultStatus.Error);
        }
    }
}

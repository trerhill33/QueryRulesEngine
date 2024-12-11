using ApprovalHierarchyManager.Application.Features.ApprovalHierarchy.CreateHierarchy.Models;
using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Hierarchys.CreateHierarchy
{
    public sealed class CreateHierarchyService : ICreateHierarchyService
    {
        private readonly IUnitOfWork<int> _unitOfWork;
        private readonly IValidator<CreateHierarchyRequest> _validator;

        public CreateHierarchyService(
            IUnitOfWork<int> unitOfWork,
            IValidator<CreateHierarchyRequest> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Result<CreateHierarchyResponse>> ExecuteAsync(
            CreateHierarchyRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate the request
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return await Result<CreateHierarchyResponse>.FailAsync(
                        errorMessages,
                        ResultStatus.Error);
                }

                // 2. Create Hierarchy entity
                var hierarchy = new Hierarchy
                {
                    Name = request.Name,
                    Description = request.Description
                };

                // 3. Execute transaction
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var hierarchyRepository = _unitOfWork.Repository<Hierarchy>();
                    var metadataKeyRepository = _unitOfWork.Repository<MetadataKey>();

                    await hierarchyRepository.AddAsync(hierarchy, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    // 4. Add default levels
                    var defaultLevels = new List<MetadataKey>
                    {
                        new MetadataKey { HierarchyId = hierarchy.Id, KeyName = "level.1" },
                        new MetadataKey { HierarchyId = hierarchy.Id, KeyName = "level.2" }
                    };

                    foreach (var level in defaultLevels)
                    {
                        await metadataKeyRepository.AddAsync(level, cancellationToken);
                    }
                }, cancellationToken);

                // 5. Construct response
                var response = new CreateHierarchyResponse
                {
                    Id = hierarchy.Id,
                    Name = hierarchy.Name,
                    Description = hierarchy.Description,
                };

                return await Result<CreateHierarchyResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                // Log the exception (Assuming a logging mechanism is in place)
                // _logger.LogError(ex, "Error creating hierarchy.");

                return await Result<CreateHierarchyResponse>.FailAsync($"Error creating hierarchy: {ex.Message}", ResultStatus.Error);
            }
        }
    }
}

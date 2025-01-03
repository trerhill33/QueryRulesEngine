﻿using FluentValidation;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Features.Hierarchies.CreateHierarchy
{
    public sealed class CreateHierarchyService(
        IHierarchyRepository hierarchyRepository,
        ILevelRepository levelRepository,
        IValidator<CreateHierarchyRequest> validator) : ICreateHierarchyService
    {
        public async Task<Result<CreateHierarchyResponse>> ExecuteAsync(
            CreateHierarchyRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate the request
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                // 2. Create Hierarchy
                var hierarchy = await hierarchyRepository.CreateHierarchyAsync(
                    request.Name,
                    request.Description,
                    request.Tag,
                    cancellationToken);

                // 3. Create default levels
                await levelRepository.CreateDefaultLevelsAsync(hierarchy.Id, cancellationToken);

                // 4. Construct response
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
                return await Result<CreateHierarchyResponse>.FailAsync(
                    $"Error creating hierarchy: {ex.Message}",
                    ResultStatus.Error);
            }
        }

        private async Task<Result<CreateHierarchyResponse>> HandleValidationFailureAsync(
             FluentValidation.Results.ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<CreateHierarchyResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}

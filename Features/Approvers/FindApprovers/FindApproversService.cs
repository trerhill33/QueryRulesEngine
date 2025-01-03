﻿using FluentValidation;
using FluentValidation.Results;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Processors;

namespace QueryRulesEngine.Features.Approvers.FindApprovers
{
    public sealed class FindApproversService(
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IQueryProcessor queryProcessor,
        IValidator<FindApproversRequest> validator) : IFindApproversService
    {
        public async Task<Result<FindApproversResponse>> ExecuteAsync(
            FindApproversRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Validate request
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailureAsync(validationResult);
                }

                // 2. Build the predicate using query processor
                var predicate = queryProcessor.BuildExpression<Employee>(request.QueryMatrix);

                // 3. Use the predicate with the repository to filter at database level
                var filteredEmployees = await readOnlyRepository
                    .FindAllByPredicateAndTransformAsync(
                        predicate,
                        e => new EmployeeDto
                        {
                            TMID = e.TMID,
                            Name = e.Name,
                            Email = e.Email,
                            Title = e.Title,
                            JobCode = e.JobCode
                        },
                        cancellationToken);


                // 4. Return response
                return await Result<FindApproversResponse>.SuccessAsync(
                    new FindApproversResponse(
                        PotentialApprovers: filteredEmployees));
            }
            catch (Exception ex)
            {
                return await Result<FindApproversResponse>.FailAsync(
                    $"Error finding approvers: {ex.Message}",
                    ResultStatus.Error);
            }
        }

        private async Task<Result<FindApproversResponse>> HandleValidationFailureAsync(
            ValidationResult validationResult)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return await Result<FindApproversResponse>.FailAsync(
                errorMessages,
                ResultStatus.Error);
        }
    }
}

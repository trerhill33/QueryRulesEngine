using ApprovalHierarchyManager.Application.Features.Employee;
using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.QueryEngine.Processors;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Approvers.FindApprovers
{
    public sealed class FindApproversService(
        IHierarchyRepository hierarchyRepository,
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

                // 2. Get employee base query
                var employeesQuery = readOnlyRepository.Entities.AsQueryable();

                // 3. Apply the query matrix
                var filteredEmployees = queryProcessor.ApplyQuery(employeesQuery, request.QueryMatrix);

                // 4. Transform to DTOs
                var potentialApprovers = await readOnlyRepository
                    .FindAllByPredicateAndTransformAsync<Employee, EmployeeDto>(
                        filteredEmployees.Expression,
                        e => new EmployeeDto
                        {
                            TMID = e.TMID,
                            Name = e.Name,
                            Email = e.Email,
                            Title = e.Title,
                            JobCode = e.JobCode
                        },
                        cancellationToken);

                // 5. Return response
                return await Result<FindApproversResponse>.SuccessAsync(
                    new FindApproversResponse(
                        PotentialApprovers: potentialApprovers));
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

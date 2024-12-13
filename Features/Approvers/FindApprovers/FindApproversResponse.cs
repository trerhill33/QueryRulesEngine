using QueryRulesEngine.dtos;

namespace QueryRulesEngine.Features.Approvers.FindApprovers
{
    public sealed record FindApproversResponse
    (
        List<EmployeeDto>? PotentialApprovers  // These are employees that match the criteria
    );
}

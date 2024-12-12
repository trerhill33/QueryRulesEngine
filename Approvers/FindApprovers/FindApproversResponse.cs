﻿using ApprovalHierarchyManager.Application.Features.Employee;

namespace QueryRulesEngine.Approvers.FindApprovers
{
    public record FindApproversResponse
    {
        List<EmployeeDto>? PotentialApprovers;  // These are employees that match the criteria
    }
}

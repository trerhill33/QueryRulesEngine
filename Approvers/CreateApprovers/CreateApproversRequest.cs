namespace QueryRulesEngine.Approvers.CreateApprovers
{
    public sealed record CreateApproversRequest(
        int HierarchyId,
        List<string> EmployeeTMIds  
    );
}

using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Approvers.FindApprovers

{
    public record FindApproversRequest(
    int HierarchyId,
    QueryMatrix QueryMatrix
    );
}

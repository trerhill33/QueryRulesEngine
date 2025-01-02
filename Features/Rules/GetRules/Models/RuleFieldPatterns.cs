namespace QueryRulesEngine.Features.Rules.GetRules.Models;
public static class RuleFieldPatterns
{
    public static class Employee
    {
        public const string Manager = "Employee.ReportsTo"; //TODO : Update to nameof
        public const string TMID = nameof(Employee.TMID);
    }

    public static class MetadataPrefix
    {
        public const string ApproverMetadataKey = "ApproverMetadataKey.";
    }
}

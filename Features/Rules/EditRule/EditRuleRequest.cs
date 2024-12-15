using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Features.Rules.EditRule;

public record EditRuleRequest
(
     int HierarchyId,
     int LevelNumber,
     string RuleNumber,
     QueryMatrix QueryMatrix 
);

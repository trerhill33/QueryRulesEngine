﻿using QueryRulesEngine.QueryEngine.Common.Models;

public record AddRuleToLevelResponse
{
    public int HierarchyId { get; init; }
    public int LevelNumber { get; init; }
    public string RuleNumber { get; init; }
    public QueryMatrix QueryMatrix { get; init; }
}



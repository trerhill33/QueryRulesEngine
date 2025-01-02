using Castle.Components.DictionaryAdapter;
using FakeItEasy;
using QueryRulesEngine.Features.Rules.GetRules;
using QueryRulesEngine.Features.Rules.GetRules.Models;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace QueryRulesEngine.Tests.Hierarchys;

[TestFixture]
public class GetRulesServiceTests
{
    private IReadOnlyRepositoryAsync<int> _readOnlyRepository;
    private IQueryPersistenceService _queryPersistenceService;
    private IGetRulesService _service;

    private static class TestData
    {
        public static class Employees
        {
            public static readonly Employee MelissaAllen = new()
            {
                TMID = "MELISSA_TMID",
                FirstName = "Melissa",
                LastName = "Allen"
            };

            public static readonly Employee BillKudlets = new()
            {
                TMID = "BILL_TMID",
                FirstName = "Bill",
                LastName = "Kudlets"
            };

            public static readonly Employee MichelleBatson = new()
            {
                TMID = "MICHELLE_TMID",
                FirstName = "Michelle",
                LastName = "Batson"
            };

            public static readonly Employee AngieLusby = new()
            {
                TMID = "ANGIE_TMID",
                FirstName = "Angie",
                LastName = "Lusby"
            };
        }

        // Retail + Corporate Rules
        public static readonly List<MetadataKey> RetailCorporateRules =
        [
            new()
            {
                HierarchyId = 1,
                KeyName = "level.1.rule.1.query:[_and][Employee.TMID_eq_@Context.RequestedTMID.ReportsTo]"
            },
            new()
            {
                HierarchyId = 1,
                KeyName = "level.2.rule.1.query:[_and][Employee.TMID_eq_MELISSA_TMID]"
            }
        ];

        // HFA Rules
        public static readonly List<MetadataKey> HfaRules =
        [
            new()
            {
                HierarchyId = 2,
                KeyName = "level.1.rule.1.query:[_and][Employee.TMID_eq_BILL_TMID]"
            }
        ];

        // Vanderbilt Rules
        public static readonly List<MetadataKey> VanderbiltRules =
        [
            new()
            {
                HierarchyId = 3,
                KeyName = "level.1.rule.1.query:[_or][Employee.TMID_eq_MICHELLE_TMID][_and][ApproverMetadataKey.BackupFor_eq_MICHELLE_TMID]"
            }
        ];

        // Manufacturing Rules
        public static readonly List<MetadataKey> ManufacturingRules =
        [
            new()
            {
                HierarchyId = 4,
                KeyName = "level.1.rule.1.query:[_or][Employee.TMID_eq_ANGIE_TMID][_and][ApproverMetadataKey.BackupFor_eq_ANGIE_TMID]"
            },
            new()
            {
                HierarchyId = 4,
                KeyName = "level.1.rule.2.query:[_and][ApproverMetadataKey.FinancialLimit_gte_2500]"
            }
        ];
    }

    [SetUp]
    public void Setup()
    {
        _readOnlyRepository = A.Fake<IReadOnlyRepositoryAsync<int>>();
        _queryPersistenceService = A.Fake<IQueryPersistenceService>();

        _service = new GetRulesService(
            _readOnlyRepository,
            _queryPersistenceService);
        SetupDynamicEmployeeLookup();
    }


    [Test]
    public async Task GetHierarchyRulesAsync_RetailCorporate_ReturnsTwoLevels()
    {
        // Arrange
        const int hierarchyId = 1;
        SetupRulesMocks(hierarchyId, TestData.RetailCorporateRules);

        A.CallTo(() => _readOnlyRepository.FindByPredicateAsNoTrackingAsync<Employee>(
        A<Expression<Func<Employee, bool>>>._,
        A<CancellationToken>._))
        .Returns(new Employee { TMID = "MELISSA_TMID", FirstName = "Melissa", LastName = "Allen" });

        // Act
        var result = await _service.ExecuteAsync(hierarchyId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Levels, Has.Count.EqualTo(2));

            // Level 1 - Manager Rule
            var level1Rules = result.Data.Levels.First(l => l.Level == 1).Rules;
            Assert.That(level1Rules.First().Configuration.IsManagerRule, Is.True);

            // Level 2 - Melissa Custom List
            var level2Rules = result.Data.Levels.First(l => l.Level == 2).Rules;
            var melissaRule = level2Rules.First();
            Assert.That(melissaRule.Configuration.IsCustomList, Is.True);

            var melissaApprover = melissaRule.Configuration.CustomListApprovers.Single();
            Assert.That(melissaApprover.TMID, Is.EqualTo("MELISSA_TMID"));
            Assert.That(melissaApprover.FirstName, Is.EqualTo("Melissa"));
            Assert.That(melissaApprover.LastName, Is.EqualTo("Allen"));
        });
    }

    [Test]
    public async Task GetHierarchyRulesAsync_HFA_ReturnsOneLevelWithBillKudlets()
    {
        // Arrange
        const int hierarchyId = 2;

        // Mock repository to return Bill for any predicate
        A.CallTo(() => _readOnlyRepository.FindByPredicateAsNoTrackingAsync<Employee>(
            A<Expression<Func<Employee, bool>>>._,
            A<CancellationToken>._))
            .Returns(new Employee
            {
                TMID = "BILL_TMID",
                FirstName = "Bill",
                LastName = "Kudlets"
            });

        // Setup rules for the hierarchy
        SetupRulesMocks(hierarchyId, TestData.HfaRules);

        // Act
        var result = await _service.ExecuteAsync(hierarchyId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Levels, Has.Count.EqualTo(1));

            var level1Rules = result.Data.Levels.First().Rules;
            var firstRule = level1Rules.First();
            Assert.That(firstRule.Configuration.IsCustomList, Is.True);

            var billApprover = firstRule.Configuration.CustomListApprovers.Single();
            Assert.That(billApprover.TMID, Is.EqualTo("BILL_TMID"));
            Assert.That(billApprover.FirstName, Is.EqualTo("Bill"));
            Assert.That(billApprover.LastName, Is.EqualTo("Kudlets"));
        });
    }

    [Test]
    public async Task GetHierarchyRulesAsync_Vanderbilt_ReturnsOneLevelWithMichelleAndBackups()
    {
        // Arrange
        const int hierarchyId = 3;
        SetupRulesMocks(hierarchyId, TestData.VanderbiltRules);

        A.CallTo(() => _readOnlyRepository.FindByPredicateAsNoTrackingAsync<Employee>(
            A<Expression<Func<Employee, bool>>>._,
            A<CancellationToken>._))
            .Returns(new Employee { TMID = "MICHELLE_TMID", FirstName = "Michelle", LastName = "Batson" });

        A.CallTo(() => _queryPersistenceService.ParseFromStorageFormat(
        A<string>.That.Contains("MICHELLE_TMID")))
            .Returns(new QueryMatrix
            {
                LogicalOperator = QueryOperator.Or,
                Conditions =
                    [
                        new QueryCondition
                        {
                            Field = RuleFieldPatterns.Employee.TMID,
                            Operator = QueryOperator.Equal,
                            Value = new(ConditionValue.Single("MICHELLE_TMID"), ConditionValueType.Single)
                        }
                    ],
                NestedMatrices =
                [
                    new QueryMatrix
                    {
                        LogicalOperator = QueryOperator.And,
                        Conditions =
                        [
                            new QueryCondition
                            {
                                Field = $"{RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey}BackupFor",
                                Operator = QueryOperator.Equal,
                                Value = new(ConditionValue.Single("MICHELLE_TMID"), ConditionValueType.Single)
                            }
                        ]
                    }
                ]
            });

        // Act
        var result = await _service.ExecuteAsync(hierarchyId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Levels, Has.Count.EqualTo(1));

            var level1Rules = result.Data.Levels.First().Rules;
            Assert.That(level1Rules, Has.Count.EqualTo(1));

            var rule = level1Rules.First();
            Assert.That(rule.Configuration.IsCustomList, Is.True);
            Assert.That(rule.Configuration.MetadataKeys, Does.Contain("BackupFor"));

            var michelleApprover = rule.Configuration.CustomListApprovers.Single();
            Assert.That(michelleApprover.TMID, Is.EqualTo("MICHELLE_TMID"));
            Assert.That(michelleApprover.FirstName, Is.EqualTo("Michelle"));
            Assert.That(michelleApprover.LastName, Is.EqualTo("Batson"));
        });
    }

    [Test]
    public async Task GetHierarchyRulesAsync_Manufacturing_ReturnsOneLevelWithAngieAndBackupsAndLimits()
    {
        // Arrange
        const int hierarchyId = 4;
        SetupRulesMocks(hierarchyId, TestData.ManufacturingRules);

        A.CallTo(() => _readOnlyRepository.FindByPredicateAsNoTrackingAsync<Employee>(
        A<Expression<Func<Employee, bool>>>._,
        A<CancellationToken>._))
        .Returns(new Employee { TMID = "ANGIE_TMID", FirstName = "Angie", LastName = "Lusby" });

        A.CallTo(() => _queryPersistenceService.ParseFromStorageFormat(
   A<string>.That.Contains("ANGIE_TMID")))
   .Returns(new QueryMatrix
   {
       LogicalOperator = QueryOperator.Or,
       Conditions =
       [
           new QueryCondition
           {
               Field = RuleFieldPatterns.Employee.TMID,
               Operator = QueryOperator.Equal,
               Value = new(ConditionValue.Single("ANGIE_TMID"), ConditionValueType.Single)
           }
       ],
       NestedMatrices =
       [
           new QueryMatrix
           {
               LogicalOperator = QueryOperator.And,
               Conditions =
               [
                   new QueryCondition
                   {
                       Field = $"{RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey}BackupFor",
                       Operator = QueryOperator.Equal,
                       Value = new(ConditionValue.Single("ANGIE_TMID"), ConditionValueType.Single)
                   }
               ]
           }
       ]
   });

        A.CallTo(() => _queryPersistenceService.ParseFromStorageFormat(
           A<string>.That.Contains("FinancialLimit")))
           .Returns(new QueryMatrix
           {
               LogicalOperator = QueryOperator.And,
               Conditions =
               [
                   new QueryCondition
           {
               Field = $"{RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey}FinancialLimit",
               Operator = QueryOperator.GreaterThanOrEqual,
               Value = new(ConditionValue.Single("2500"), ConditionValueType.Single)
           }
               ]
           });

        // Act
        var result = await _service.ExecuteAsync(hierarchyId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data.Levels, Has.Count.EqualTo(1));

            var level1Rules = result.Data.Levels.First().Rules.ToList();
            Assert.That(level1Rules, Has.Count.EqualTo(2));

            // Rule 1 - Angie and Backups
            var primaryRule = level1Rules.First();
            Assert.That(primaryRule.Configuration.IsCustomList, Is.True);
            Assert.That(primaryRule.Configuration.MetadataKeys, Does.Contain("BackupFor"));

            var angieApprover = primaryRule.Configuration.CustomListApprovers.Single();
            Assert.That(angieApprover.TMID, Is.EqualTo("ANGIE_TMID"));
            Assert.That(angieApprover.FirstName, Is.EqualTo("Angie"));
            Assert.That(angieApprover.LastName, Is.EqualTo("Lusby"));

            // Rule 2 - Financial Limit
            var limitRule = level1Rules.Last();
            Assert.That(limitRule.Configuration.MetadataKeys, Does.Contain("FinancialLimit"));
        });
    }

    [Test]
    public async Task GetHierarchyRulesAsync_NoRulesFound_ReturnsFailure()
    {
        // Arrange
        const int hierarchyId = 999;
        SetupRulesMocks(hierarchyId, []);

        // Act
        var result = await _service.ExecuteAsync(hierarchyId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Messages, Does.Contain($"No rules found for hierarchy {hierarchyId}"));
        });
    }

    private void SetupRulesMocks(int hierarchyId, List<MetadataKey> rules)
    {
        A.CallTo(() => _readOnlyRepository.FindAllByPredicateAsync<MetadataKey>(
            A<Expression<Func<MetadataKey, bool>>>._,
            A<CancellationToken>._))
            .Returns(rules);

        foreach (var rule in rules)
        {
            var queryMatrix = CreateQueryMatrixForRule(rule.KeyName);
            A.CallTo(() => _queryPersistenceService.ParseFromStorageFormat(rule.KeyName))
                .Returns(queryMatrix);
        }
    }

    private static QueryMatrix CreateQueryMatrixForRule(string ruleKey)
    {
        // Manager Rule (ReportsTo)
        if (ruleKey.Contains("RequestedTMID.ReportsTo"))
        {
            return new QueryMatrix
            {
                LogicalOperator = QueryOperator.And,
                Conditions =
                [
                    new QueryCondition
                {
                    Field = RuleFieldPatterns.Employee.Manager,  // Use constant
                    Operator = QueryOperator.Equal,
                    Value = new(ConditionValue.Single("@Context.RequestedTMID"), ConditionValueType.Single)
                }
                ]
            };
        }

        // Custom List (Direct TMID check)
        if (ruleKey.Contains("TMID_eq_"))
        {
            var tmidMatch = Regex.Match(ruleKey, @"TMID_eq_(\w+)");
            var tmid = tmidMatch.Success ? tmidMatch.Groups[1].Value : "";

            return new QueryMatrix
            {
                LogicalOperator = QueryOperator.And,
                Conditions =
                [
                    new QueryCondition
                {
                    Field = RuleFieldPatterns.Employee.TMID,  // Use constant
                    Operator = QueryOperator.Equal,
                    Value = new(ConditionValue.Single(tmid), ConditionValueType.Single)
                }
                ]
            };
        }

        // Backup Rule
        if (ruleKey.Contains("BackupFor"))
        {
            var tmidMatch = Regex.Match(ruleKey, @"BackupFor_eq_(\w+)");
            var tmid = tmidMatch.Success ? tmidMatch.Groups[1].Value : "";

            return new QueryMatrix
            {
                LogicalOperator = QueryOperator.And,
                Conditions =
                [
                    new QueryCondition
                {
                    Field = $"{RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey}BackupFor",
                    Operator = QueryOperator.Equal,
                    Value = new(ConditionValue.Single(tmid), ConditionValueType.Single)
                }
                ]
            };
        }

        // For cases with nested OR conditions (like Vanderbilt and Manufacturing)
        if (ruleKey.Contains("[_or]"))
        {
            if (ruleKey.Contains("MICHELLE_TMID"))
            {
                return new QueryMatrix
                {
                    LogicalOperator = QueryOperator.Or,
                    Conditions = [],
                    NestedMatrices =
                    [
                        // Primary approver matrix
                        new QueryMatrix
                    {
                        LogicalOperator = QueryOperator.And,
                        Conditions =
                        [
                            new QueryCondition
                            {
                                Field = RuleFieldPatterns.Employee.TMID,
                                Operator = QueryOperator.Equal,
                                Value = new(ConditionValue.Single("MICHELLE_TMID"), ConditionValueType.Single)
                            }
                        ]
                    },
                    // Backup matrix
                    new QueryMatrix
                    {
                        LogicalOperator = QueryOperator.And,
                        Conditions =
                        [
                            new QueryCondition
                            {
                                Field = $"{RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey}BackupFor",
                                Operator = QueryOperator.Equal,
                                Value = new(ConditionValue.Single("MICHELLE_TMID"), ConditionValueType.Single)
                            }
                        ]
                    }
                    ]
                };
            }
            else if (ruleKey.Contains("ANGIE_TMID"))
            {
                return new QueryMatrix
                {
                    LogicalOperator = QueryOperator.Or,
                    Conditions = [],
                    NestedMatrices =
                    [
                        // Primary approver matrix
                        new QueryMatrix
                    {
                        LogicalOperator = QueryOperator.And,
                        Conditions =
                        [
                            new QueryCondition
                            {
                                Field = RuleFieldPatterns.Employee.TMID,
                                Operator = QueryOperator.Equal,
                                Value = new(ConditionValue.Single("ANGIE_TMID"), ConditionValueType.Single)
                            }
                        ]
                    },
                    // Backup matrix
                    new QueryMatrix
                    {
                        LogicalOperator = QueryOperator.And,
                        Conditions =
                        [
                            new QueryCondition
                            {
                                Field = $"{RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey}BackupFor",
                                Operator = QueryOperator.Equal,
                                Value = new(ConditionValue.Single("ANGIE_TMID"), ConditionValueType.Single)
                            }
                        ]
                    }
                    ]
                };
            }
        }

        // Default case
        return new QueryMatrix
        {
            LogicalOperator = QueryOperator.And,
            Conditions = []
        };
    }


    private void SetupDynamicEmployeeLookup()
    {
        A.CallTo(() => _readOnlyRepository.FindByPredicateAsNoTrackingAsync<Employee>(
            A<Expression<Func<Employee, bool>>>._,
            A<CancellationToken>._))
        .ReturnsLazily((Expression<Func<Employee, bool>> predicate, CancellationToken _) =>
        {
            // Sample employees to simulate database
            var employees = new List<Employee>
            {
            new Employee { TMID = "BILL_TMID", FirstName = "Bill", LastName = "Kudlets" },
            new Employee { TMID = "MELISSA_TMID", FirstName = "Melissa", LastName = "Allen" },
            new Employee { TMID = "MICHELLE_TMID", FirstName = "Michelle", LastName = "Batson" },
            new Employee { TMID = "ANGIE_TMID", FirstName = "Angie", LastName = "Lusby" }
            };

            // Evaluate the predicate against the list
            return employees.AsQueryable().FirstOrDefault(predicate);
        });
    }

}
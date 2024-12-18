﻿using Moq;
using QueryRulesEngine.dtos;
using QueryRulesEngine.Features.MetadataKeys.MetaDataGridBuilder;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;
using System.Linq.Expressions;

namespace QueryRulesEngine.Tests.Hierarchys.Metadatakeys
{
    [TestFixture]
    public class MetadataKeyQueryServiceTests
    {
        private static class TestData
        {
            public const string MetadataKey = "ApproverMetadataKey.FinancialLimits";

            public static readonly List<HierarchyInfo> Hierarchies =
        [
            new() { Id = 1, Name = "Vanderbilt", Tag = "Vanderbilt" },
            new() { Id = 2, Name = "Vanderbilt - Retail", Tag = "Vanderbilt" },
            new() { Id = 3, Name = "Vanderbilt - Business", Tag = "Vanderbilt" },
            new() { Id = 4, Name = "Manufacturing", Tag = "Manufacturing" },
            new() { Id = 5, Name = "HFA", Tag = "HFA" }
        ];

            public static readonly List<ApproverMetadataDto> ApproverData =
        [
            new()
            {
                ApproverId = "331220",
                ApproverName = "John Smith",
                HierarchyId = 1,
                Value = "10000"
            },
            new()
            {
                ApproverId = "331220",
                ApproverName = "John Smith",
                HierarchyId = 2,
                Value = "25000"
            },
            new()
            {
                ApproverId = "331220",
                ApproverName = "John Smith",
                HierarchyId = 3,
                Value = "15000"
            },
            new()
            {
                ApproverId = "331278",
                ApproverName = "Jane Manager",
                HierarchyId = 1,
                Value = "50000"
            },
            new()
            {
                ApproverId = "331278",
                ApproverName = "Jane Manager",
                HierarchyId = 4,
                Value = "75000"
            }
        ];

            public static List<HierarchyTagGroup> ExpectedTagGroups =>
        [
            new HierarchyTagGroup
            {
                Tag = "Vanderbilt",
                Hierarchies = Hierarchies.Where(h => h.Tag == "Vanderbilt").ToList()
            },
            new HierarchyTagGroup
            {
                Tag = "Manufacturing",
                Hierarchies = Hierarchies.Where(h => h.Tag == "Manufacturing").ToList()
            },
            new HierarchyTagGroup
            {
                Tag = "HFA",
                Hierarchies = Hierarchies.Where(h => h.Tag == "HFA").ToList()
            }
        ];
        }

        private Mock<IReadOnlyRepositoryAsync<int>> _readOnlyRepositoryMock;
        private Mock<IApproverMetadataRepository> _approverMetadataRepositoryMock;
        private MetadataKeyQueryService _queryService;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Setup()
        {
            _readOnlyRepositoryMock = new Mock<IReadOnlyRepositoryAsync<int>>(MockBehavior.Strict);
            _approverMetadataRepositoryMock = new Mock<IApproverMetadataRepository>(MockBehavior.Strict);
            _cancellationToken = CancellationToken.None;

            _queryService = new MetadataKeyQueryService(
                _readOnlyRepositoryMock.Object,
                _approverMetadataRepositoryMock.Object);
        }

        [Test]
        public async Task GetMetadataValuesForKey_ValidKey_ReturnsGroupedHierarchiesAndValues()
        {
            // Arrange
            SetupSuccessScenario();

            // Act
            var result = await _queryService.GetMetadataValuesForKeyAsync(TestData.MetadataKey, _cancellationToken);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);

                var response = result.Data;

                // Verify tag groups
                Assert.That(response.TagGroups, Has.Count.EqualTo(3), "Should have 3 tag groups");

                var vanderbiltGroup = response.TagGroups.First(g => g.Tag == "Vanderbilt");
                Assert.That(vanderbiltGroup.Hierarchies, Has.Count.EqualTo(3),
                    "Vanderbilt tag should have 3 hierarchies");

                // Verify approver data
                Assert.That(response.Data, Has.Count.EqualTo(2), "Should have 2 unique approvers");

                var johnSmith = response.Data.First(d => d.ApproverId == "331220");
                Assert.That(johnSmith.HierarchyValues, Has.Count.EqualTo(3),
                    "John Smith should have values in 3 Vanderbilt hierarchies");

                var janeManager = response.Data.First(d => d.ApproverId == "331278");
                Assert.That(janeManager.HierarchyValues, Has.Count.EqualTo(2),
                    "Jane Manager should have values in 2 hierarchies");
            });

            VerifyMockInteractions();
        }

        [Test]
        public async Task GetMetadataValuesForKey_NoMatchingTag_ReturnsEmptyGroup()
        {
            // Arrange
            var hierarchies = new List<HierarchyInfo>
        {
            new() { Id = 1, Name = "Test Hierarchy", Tag = "UnusedTag" }
        };

            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsNoTrackingAndTransformAsync<MetadataKey, HierarchyInfo>(
                    It.IsAny<Expression<Func<MetadataKey, bool>>>(),
                    It.IsAny<Expression<Func<MetadataKey, HierarchyInfo>>>(),
                    _cancellationToken))
                .ReturnsAsync(hierarchies);

            _approverMetadataRepositoryMock
                .Setup(r => r.GetApproverMetadataValuesAsync(
                    TestData.MetadataKey,
                    It.IsAny<IEnumerable<int>>(),
                    _cancellationToken))
                .ReturnsAsync([]);

            // Act
            var result = await _queryService.GetMetadataValuesForKeyAsync(TestData.MetadataKey, _cancellationToken);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Data.TagGroups, Has.Count.EqualTo(1));
                Assert.That(result.Data.Data, Is.Empty);
            });
        }

        private void SetupSuccessScenario()
        {
            _readOnlyRepositoryMock
                .Setup(r => r.FindAllByPredicateAsNoTrackingAndTransformAsync<MetadataKey, HierarchyInfo>(
                    It.IsAny<Expression<Func<MetadataKey, bool>>>(),
                    It.IsAny<Expression<Func<MetadataKey, HierarchyInfo>>>(),
                    _cancellationToken))
                .ReturnsAsync(TestData.Hierarchies);

            _approverMetadataRepositoryMock
                .Setup(r => r.GetApproverMetadataValuesAsync(
                    TestData.MetadataKey,
                    It.IsAny<IEnumerable<int>>(),
                    _cancellationToken))
                .ReturnsAsync(TestData.ApproverData);
        }

        private void VerifyMockInteractions()
        {
            _readOnlyRepositoryMock.Verify(
                r => r.FindAllByPredicateAsNoTrackingAndTransformAsync<MetadataKey, HierarchyInfo>(
                    It.IsAny<Expression<Func<MetadataKey, bool>>>(),
                    It.IsAny<Expression<Func<MetadataKey, HierarchyInfo>>>(),
                    _cancellationToken),
                Times.Once);

            _approverMetadataRepositoryMock.Verify(
                r => r.GetApproverMetadataValuesAsync(
                    TestData.MetadataKey,
                    It.IsAny<IEnumerable<int>>(),
                    _cancellationToken),
                Times.Once);
        }
    }

}

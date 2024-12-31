using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Processors;

public interface IMetadataQueryProcessor
{
    Expression<Func<T, bool>> BuildExpressionFromQueryMatrix<T>(
        QueryMatrix matrix,
        string? hierarchyId = null,
        string? context = null)
        where T : class;
    Expression<Func<Employee, object>>[] GetRequiredIncludes();
}
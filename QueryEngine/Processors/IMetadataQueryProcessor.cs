using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Processors;

public interface IMetadataQueryProcessor
{
    Expression<Func<T, bool>> BuildExpression<T>(QueryMatrix matrix, string context = "") where T : class;
    Expression<Func<Employee, object>>[] GetRequiredIncludes();
}
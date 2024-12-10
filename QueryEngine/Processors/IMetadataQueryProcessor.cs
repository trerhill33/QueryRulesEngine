using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Processors
{
    public interface IMetadataQueryProcessor
    {
        IQueryable<T> ApplyQuery<T>(IQueryable<T> source, QueryMatrix matrix) where T : class;
        Expression<Func<T, bool>> BuildExpression<T>(QueryMatrix matrix) where T : class;
    }
}
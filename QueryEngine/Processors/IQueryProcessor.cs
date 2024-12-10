using QueryRulesEngine.QueryEngine.Common.Models;
using System.Linq.Expressions;

namespace QueryRulesEngine.QueryEngine.Processors
{
    public interface IQueryProcessor
    {
        IQueryable<T> ApplyQuery<T>(IQueryable<T> source, QueryMatrix matrix);
        Expression<Func<T, bool>> BuildExpression<T>(QueryMatrix matrix);
    }
}
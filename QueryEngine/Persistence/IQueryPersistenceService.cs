using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.QueryEngine.Persistence
{
    public interface IQueryPersistenceService
    {
        string ConvertToStorageFormat(QueryMatrix matrix);
        QueryMatrix ParseFromStorageFormat(string queryString);
    }
}
